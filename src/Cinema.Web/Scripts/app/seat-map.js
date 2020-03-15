/*!
 * Interactive seat map for the Lumiere booking pages.
 * jQuery plugin: $('#seat-map').seatMap({ seatsUrl, holdUrl, releaseUrl, maxSeats, token })
 *
 * Flow: poll the seat map -> customer toggles seats locally -> POST hold -> countdown until
 * the hold expires -> checkout link. Any 409 from the server (seat taken, stale map)
 * shows the message and refreshes the plan.
 */
(function ($, window) {
    'use strict';

    var POLL_MS = 15000;
    var URGENT_SECONDS = 60;

    var currency = function (value) {
        return '£' + Number(value).toFixed(2);
    };

    function SeatMap(element, options) {
        this.$el = $(element);
        this.options = $.extend({ maxSeats: 10 }, options);
        this.selected = {};          // seatId -> seat
        this.hold = null;            // { id, expiresAt: Date, checkoutUrl }
        this.version = null;
        this.pollTimer = null;
        this.countdownTimer = null;

        this.$list = $('#selection-list');
        this.$empty = $('#selection-empty');
        this.$total = $('#selection-total');
        this.$error = $('#seat-error');
        this.$holdButton = $('#hold-button');
        this.$pending = $('#hold-pending');
        this.$active = $('#hold-active');
        this.$timer = $('#hold-timer');
        this.$checkout = $('#checkout-link');

        this.bind();
        this.refresh();
    }

    SeatMap.prototype.bind = function () {
        var self = this;

        this.$el.on('click', 'button.seat', function () {
            self.toggle($(this));
        });

        this.$holdButton.on('click', function () {
            self.placeHold();
        });

        $('#release-button').on('click', function () {
            self.releaseHold();
        });

        $(window.document).on('visibilitychange', function () {
            if (!window.document.hidden) {
                self.refresh();
            }
        });
    };

    SeatMap.prototype.ajax = function (method, url, data) {
        return $.ajax({
            type: method,
            url: url,
            data: data ? JSON.stringify(data) : undefined,
            contentType: 'application/json; charset=utf-8',
            dataType: method === 'GET' ? 'json' : undefined,
            cache: false,
            headers: {
                'RequestVerificationToken': this.options.token,
                'X-Requested-With': 'XMLHttpRequest'
            }
        });
    };

    SeatMap.prototype.refresh = function () {
        var self = this;
        window.clearTimeout(this.pollTimer);

        return this.ajax('GET', this.options.seatsUrl)
            .done(function (map) {
                self.version = map.SeatVersion;
                if (map.holdId && map.holdExpiresUtc) {
                    self.startHold({ holdId: map.holdId, expiresUtc: map.holdExpiresUtc });
                }
                self.render(map);
            })
            .fail(function () {
                self.showError('Could not load the seating plan. Retrying…');
            })
            .always(function () {
                self.pollTimer = window.setTimeout(function () { self.refresh(); }, POLL_MS);
            });
    };

    SeatMap.prototype.render = function (map) {
        var self = this;
        var $rows = $('<div/>');
        var stillSelectable = {};

        $.each(map.rows, function (_, row) {
            var $row = $('<div class="seat-row"/>');
            var aisleAfter = row.seats.length >= 10 ? Math.floor(row.seats.length / 2) : -1;
            $row.append($('<span class="seat-row-label"/>').text(row.row));

            $.each(row.seats, function (i, seat) {
                var classes = ['seat', 'seat-' + seat.type.toLowerCase()];
                var disabled = false;

                switch (seat.state) {
                    case 'Sold':
                        classes.push('seat-sold');
                        disabled = true;
                        break;
                    case 'Held':
                        classes.push('seat-held');
                        disabled = true;
                        break;
                    case 'HeldByYou':
                        classes.push('seat-mine');
                        stillSelectable[seat.id] = seat;
                        break;
                    default:
                        classes.push('seat-available');
                        if (self.selected[seat.id]) {
                            classes.push('seat-selected');
                            stillSelectable[seat.id] = seat;
                        }
                }

                if (i === aisleAfter) {
                    classes.push('seat-aisle');
                }

                var title = seat.label + ' – ' + seat.type + ' ' + currency(seat.price) +
                    (disabled ? ' (unavailable)' : '');

                $('<button type="button"/>')
                    .addClass(classes.join(' '))
                    .attr({ title: title, 'aria-label': title, 'aria-pressed': !!stillSelectable[seat.id] })
                    .prop('disabled', disabled || (self.hold !== null && seat.state !== 'HeldByYou'))
                    .data('seat', seat)
                    .text(seat.number)
                    .appendTo($row);
            });

            $rows.append($row);
        });

        // Seats someone else grabbed since the customer clicked them silently drop out.
        this.selected = stillSelectable;
        this.$el.empty().append($rows.children());
        this.renderSummary();
    };

    SeatMap.prototype.toggle = function ($seat) {
        if (this.hold) {
            return;
        }

        var seat = $seat.data('seat');
        this.hideError();

        if (this.selected[seat.id]) {
            delete this.selected[seat.id];
            $seat.removeClass('seat-selected').attr('aria-pressed', false);
        } else {
            if (this.count() >= this.options.maxSeats) {
                this.showError('You can book up to ' + this.options.maxSeats + ' seats at a time.');
                return;
            }
            this.selected[seat.id] = seat;
            $seat.addClass('seat-selected').attr('aria-pressed', true);
        }

        this.renderSummary();
    };

    SeatMap.prototype.count = function () {
        return $.map(this.selected, function (s) { return s; }).length;
    };

    SeatMap.prototype.renderSummary = function () {
        var seats = $.map(this.selected, function (s) { return s; });
        seats.sort(function (a, b) { return a.label.localeCompare(b.label, undefined, { numeric: true }); });

        var total = 0;
        this.$list.empty();
        $.each(seats, function (_, s) {
            total += s.price;
            $('<li/>')
                .append($('<span/>').text(s.label + ' · ' + s.type))
                .append($('<span class="pull-right"/>').text(currency(s.price)))
                .appendTo(this.$list);
        }.bind(this));

        this.$empty.toggleClass('hidden', seats.length > 0);
        this.$total.text(currency(total));
        this.$holdButton.prop('disabled', seats.length === 0);
    };

    SeatMap.prototype.placeHold = function () {
        var self = this;
        var ids = $.map(this.selected, function (s) { return s.id; });
        if (!ids.length) {
            return;
        }

        this.$holdButton.prop('disabled', true).text('Holding…');

        this.ajax('POST', this.options.holdUrl, { seatIds: ids })
            .done(function (result) {
                self.hideError();
                self.startHold(result);
                self.refresh();
            })
            .fail(function (xhr) {
                self.showError(self.errorText(xhr));
                if (xhr.status === 409) {
                    self.refresh();
                }
            })
            .always(function () {
                self.$holdButton.text('Hold these seats').prop('disabled', self.count() === 0);
            });
    };

    SeatMap.prototype.releaseHold = function () {
        var self = this;
        if (!this.hold) {
            return;
        }

        this.ajax('POST', this.options.releaseUrl, { holdId: this.hold.id })
            .always(function () {
                self.endHold();
                self.refresh();
            });
    };

    SeatMap.prototype.startHold = function (result) {
        var self = this;
        this.hold = {
            id: result.holdId,
            expiresAt: new Date(result.expiresUtc || result.holdExpiresUtc),
            checkoutUrl: result.checkoutUrl || this.$checkout.attr('href')
        };

        if (result.checkoutUrl) {
            this.$checkout.attr('href', result.checkoutUrl);
        } else if (this.$checkout.attr('href') === '#') {
            this.$checkout.attr('href', '/bookings/checkout?holdId=' + encodeURIComponent(result.holdId));
        }

        this.$pending.addClass('hidden');
        this.$active.removeClass('hidden');

        window.clearInterval(this.countdownTimer);
        this.countdownTimer = window.setInterval(function () { self.tick(); }, 1000);
        this.tick();
    };

    SeatMap.prototype.tick = function () {
        var seconds = Math.max(0, Math.round((this.hold.expiresAt - new Date()) / 1000));
        var mm = Math.floor(seconds / 60);
        var ss = seconds % 60;

        this.$timer.text(mm + ':' + (ss < 10 ? '0' : '') + ss).toggleClass('urgent', seconds <= URGENT_SECONDS);

        if (seconds === 0) {
            this.endHold();
            this.selected = {};
            this.showError('Your hold expired and the seats were released. Please choose again.');
            this.refresh();
        }
    };

    SeatMap.prototype.endHold = function () {
        window.clearInterval(this.countdownTimer);
        this.hold = null;
        this.$active.addClass('hidden');
        this.$pending.removeClass('hidden');
    };

    SeatMap.prototype.errorText = function (xhr) {
        if (xhr.responseJSON && xhr.responseJSON.error) {
            return xhr.responseJSON.error;
        }
        return 'Something went wrong. Please try again.';
    };

    SeatMap.prototype.showError = function (message) {
        this.$error.text(message).removeClass('hidden');
    };

    SeatMap.prototype.hideError = function () {
        this.$error.addClass('hidden').text('');
    };

    $.fn.seatMap = function (options) {
        return this.each(function () {
            if (!$.data(this, 'seatMap')) {
                $.data(this, 'seatMap', new SeatMap(this, options));
            }
        });
    };
}(jQuery, window));
