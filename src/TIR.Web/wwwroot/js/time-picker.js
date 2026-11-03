(function ($) {
    'use strict';

    // ورودی ساعت و تاریخ و ساعت همیشه از یک انتخابگر استفاده می کنند.
    function createPicker() {
        var backdrop = $('<div class="tir-clock-backdrop" role="presentation"></div>').hide().appendTo('body');
        var dialog = $('<div class="tir-clock-dialog" role="dialog" aria-modal="true" aria-label="انتخاب ساعت"></div>').appendTo(backdrop);
        var target = null, callback = null, hour = 0, minute = 0, stage = 'hour', keyboardMode = false;
        var tehranClock = new Intl.DateTimeFormat('en-GB', {
            timeZone: 'Asia/Tehran', hour: '2-digit', minute: '2-digit', hourCycle: 'h23'
        });

        function pad(number) { return String(number).padStart(2, '0'); }
        function normalizeDigits(value) {
            return String(value || '').replace(/[۰-۹٠-٩]/g, function (digit) {
                var code = digit.charCodeAt(0);
                return String.fromCharCode(48 + code - (code >= 0x06f0 ? 0x06f0 : 0x0660));
            });
        }
        function currentTime() {
            var result = {};
            tehranClock.formatToParts(new Date()).forEach(function (part) {
                if (part.type === 'hour' || part.type === 'minute') result[part.type] = Number(part.value);
            });
            return result;
        }
        function readTime(value) {
            var match = normalizeDigits(value).match(/(?:^|\s)(\d{1,2}):(\d{1,2})(?::\d{2})?(?=\s|$)/);
            if (match && Number(match[1]) < 24 && Number(match[2]) < 60) {
                return { hour: Number(match[1]), minute: Number(match[2]) };
            }
            return currentTime();
        }
        function displayHour() { return hour % 12 || 12; }
        function period() { return hour < 12 ? 'AM' : 'PM'; }
        function close() {
            backdrop.hide();
            target = null;
            callback = null;
        }
        function readManualValues() {
            var hourInput = dialog.find('.tir-clock-manual__hour');
            var minuteInput = dialog.find('.tir-clock-manual__minute');
            var newHour = Number(normalizeDigits(hourInput.val()));
            var newMinute = Number(normalizeDigits(minuteInput.val()));
            var valid = hourInput.val() !== '' && minuteInput.val() !== '' &&
                Number.isInteger(newHour) && Number.isInteger(newMinute) &&
                newHour >= 1 && newHour <= 12 && newMinute >= 0 && newMinute <= 59;
            dialog.find('.tir-clock-manual__error').toggle(!valid);
            if (!valid) return false;
            hour = newHour % 12 + (period() === 'PM' ? 12 : 0);
            minute = newMinute;
            return true;
        }
        function save() {
            if (keyboardMode && !readManualValues()) return;
            var handler = callback;
            var selected = pad(hour) + ':' + pad(minute);
            close();
            if (handler) handler(selected);
        }
        function setPeriod(value) {
            if (keyboardMode && !readManualValues()) return;
            hour = hour % 12 + (value === 'PM' ? 12 : 0);
            render();
        }
        function changeSelection(selected) {
            if (stage === 'hour') {
                hour = selected % 12 + (period() === 'PM' ? 12 : 0);
                stage = 'minute';
            } else {
                minute = selected;
            }
            render();
        }
        function appendDial(container) {
            var dial = $('<div class="tir-clock-dial" role="group"></div>')
                .attr('aria-label', stage === 'hour' ? 'انتخاب ساعت از صفحه ساعت' : 'انتخاب دقیقه از صفحه ساعت')
                .appendTo(container);
            var current = stage === 'hour' ? displayHour() % 12 : minute;
            var angle = stage === 'hour' ? current * 30 : current * 6;
            $('<div class="tir-clock-dial__hand"></div>')
                .css('transform', 'rotate(' + angle + 'deg)').appendTo(dial);
            $('<span class="tir-clock-dial__center"></span>').appendTo(dial);

            for (var slot = 0; slot < 12; slot++) {
                (function (index) {
                    var value = stage === 'hour' ? (index || 12) : index * 5;
                    var radians = index * Math.PI / 6;
                    var selected = value === (stage === 'hour' ? displayHour() : minute);
                    $('<button type="button" class="tir-clock-dial__number"></button>')
                        .text(stage === 'hour' ? value : pad(value))
                        .attr('aria-label', (stage === 'hour' ? 'ساعت ' : 'دقیقه ') + value)
                        .attr('aria-pressed', String(selected))
                        .toggleClass('is-selected', selected)
                        .css({
                            left: (50 + 39 * Math.sin(radians)) + '%',
                            top: (50 - 39 * Math.cos(radians)) + '%'
                        })
                        .on('click', function (event) {
                            event.stopPropagation();
                            changeSelection(value);
                        }).appendTo(dial);
                })(slot);
            }

            // دقیقه های بین نشانگرهای پنج دقیقه ای نیز از روی صفحه ساعت قابل انتخاب هستند.
            if (stage === 'minute' && minute % 5 !== 0) {
                var radians = minute * Math.PI / 30;
                $('<span class="tir-clock-dial__precise is-selected"></span>')
                    .text(pad(minute))
                    .css({
                        left: (50 + 39 * Math.sin(radians)) + '%',
                        top: (50 - 39 * Math.cos(radians)) + '%'
                    }).appendTo(dial);
            }

            dial.on('click', function (event) {
                var bounds = this.getBoundingClientRect();
                var dx = event.clientX - (bounds.left + bounds.width / 2);
                var dy = event.clientY - (bounds.top + bounds.height / 2);
                if (Math.hypot(dx, dy) < 17) return;
                var degrees = (Math.atan2(dx, -dy) * 180 / Math.PI + 360) % 360;
                changeSelection(stage === 'hour'
                    ? (Math.round(degrees / 30) % 12 || 12)
                    : Math.round(degrees / 6) % 60);
            });
        }
        function appendManual(container) {
            var manual = $('<div class="tir-clock-manual" dir="ltr"></div>').appendTo(container);
            var hourBox = $('<div class="tir-clock-manual__field"></div>').appendTo(manual);
            $('<label for="tirClockHour">ساعت</label>').appendTo(hourBox);
            $('<input id="tirClockHour" class="tir-clock-manual__hour form-control" type="number" min="1" max="12" inputmode="numeric" />')
                .val(pad(displayHour())).appendTo(hourBox);
            $('<span class="tir-clock-manual__separator">:</span>').appendTo(manual);
            var minuteBox = $('<div class="tir-clock-manual__field"></div>').appendTo(manual);
            $('<label for="tirClockMinute">دقیقه</label>').appendTo(minuteBox);
            $('<input id="tirClockMinute" class="tir-clock-manual__minute form-control" type="number" min="0" max="59" inputmode="numeric" />')
                .val(pad(minute)).appendTo(minuteBox);
            $('<div class="tir-clock-manual__error" role="alert">ساعت باید بین 1 تا 12 و دقیقه بین 0 تا 59 باشد.</div>')
                .hide().appendTo(container);
        }
        function render() {
            dialog.empty();
            $('<div class="tir-clock-caption">انتخاب ساعت</div>').appendTo(dialog);
            var heading = $('<div class="tir-clock-heading" dir="ltr"></div>').appendTo(dialog);
            var digits = $('<div class="tir-clock-digits"></div>').appendTo(heading);
            $('<button type="button" class="tir-clock-digits__part"></button>')
                .text(pad(displayHour())).toggleClass('is-active', stage === 'hour')
                .attr('aria-label', 'انتخاب ساعت')
                .on('click', function () { keyboardMode = false; stage = 'hour'; render(); })
                .appendTo(digits);
            $('<span class="tir-clock-digits__separator">:</span>').appendTo(digits);
            $('<button type="button" class="tir-clock-digits__part"></button>')
                .text(pad(minute)).toggleClass('is-active', stage === 'minute')
                .attr('aria-label', 'انتخاب دقیقه')
                .on('click', function () { keyboardMode = false; stage = 'minute'; render(); })
                .appendTo(digits);
            var periods = $('<div class="tir-clock-periods" role="group" aria-label="قبل از ظهر یا بعد از ظهر"></div>')
                .appendTo(heading);
            ['AM', 'PM'].forEach(function (value) {
                $('<button type="button" class="tir-clock-periods__button"></button>')
                    .text(value).toggleClass('is-active', period() === value)
                    .attr('aria-label', value === 'AM' ? 'قبل از ظهر' : 'بعد از ظهر')
                    .attr('aria-pressed', String(period() === value))
                    .on('click', function () { setPeriod(value); }).appendTo(periods);
            });
            var body = $('<div class="tir-clock-body"></div>').appendTo(dialog);
            if (keyboardMode) appendManual(body);
            else appendDial(body);

            var footer = $('<div class="tir-clock-footer" dir="ltr"></div>').appendTo(dialog);
            $('<button type="button" class="tir-clock-keyboard" aria-label="تغییر روش انتخاب ساعت"></button>')
                .attr('title', keyboardMode ? 'انتخاب از صفحه ساعت' : 'ورود دستی ساعت')
                .text(keyboardMode ? '◷' : '⌨')
                .on('click', function () {
                    if (keyboardMode && !readManualValues()) return;
                    keyboardMode = !keyboardMode;
                    render();
                    if (keyboardMode) dialog.find('.tir-clock-manual__hour').trigger('focus');
                }).appendTo(footer);
            $('<div class="tir-clock-footer__actions"></div>')
                .append($('<button type="button" class="tir-clock-footer__button"></button>').text('انصراف').on('click', close))
                .append($('<button type="button" class="tir-clock-footer__button"></button>').text('تایید').on('click', save))
                .appendTo(footer);
        }
        function open(input, options) {
            options = options || {};
            target = input;
            callback = options.onSelect;
            var initial = readTime(options.value);
            hour = initial.hour;
            minute = initial.minute;
            stage = 'hour';
            keyboardMode = false;
            render();
            backdrop.show();
            dialog.find('.tir-clock-digits__part').first().trigger('focus');
        }
        backdrop.on('mousedown', function (event) {
            if (event.target === backdrop[0]) close();
        });
        $(document).on('keydown.tirClockPicker', function (event) {
            if (!backdrop.is(':visible')) return;
            if (event.key === 'Escape') { event.preventDefault(); close(); }
            if (event.key === 'Enter' && $(event.target).closest('.tir-clock-manual').length) {
                event.preventDefault();
                save();
            }
        });

        return {
            open: open,
            close: close,
            isOpenFor: function (input) {
                return backdrop.is(':visible') && target && input && target[0] === input[0];
            }
        };
    }
    window.TirTimePicker = { createPicker: createPicker };
})(jQuery);
