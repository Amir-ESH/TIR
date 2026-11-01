(function ($) {
    'use strict';

    $(function () {
        var root = $('#tirIranClock');
        if (!root.length) return;

        var DAY = 24 * 60 * 60 * 1000;
        var dateText = $('#tirIranToday');
        var timeText = $('#tirIranTime');
        var trigger = $('#tirIranDateTrigger');
        var calendar = $('#tirIranCalendar');
        var monthTitle = $('#tirIranMonthTitle');
        var daysGrid = $('#tirIranCalendarDays');
        var visibleMonthStart = null;
        var lastLocalDay = null;

        try {
            // همه تاریخ ها و ساعت ها با منطقه زمانی تهران خوانده می شوند.
            // محاسبه روزهای ماه با تقویم رسمی Persian در Intl انجام می شود.
            var tehranGregorian = new Intl.DateTimeFormat('en-US-u-nu-latn', {
                timeZone: 'Asia/Tehran', year: 'numeric', month: 'numeric', day: 'numeric'
            });
            var persianPartsFormatter = new Intl.DateTimeFormat('fa-IR-u-ca-persian-nu-latn', {
                timeZone: 'UTC', year: 'numeric', month: 'numeric', day: 'numeric'
            });
            var dateFormatter = new Intl.DateTimeFormat('fa-IR-u-ca-persian', {
                timeZone: 'Asia/Tehran', weekday: 'long', year: 'numeric', month: 'long', day: 'numeric'
            });
            var timeFormatter = new Intl.DateTimeFormat('fa-IR', {
                timeZone: 'Asia/Tehran', hour: '2-digit', minute: '2-digit', second: '2-digit',
                hourCycle: 'h23'
            });
            var monthFormatter = new Intl.DateTimeFormat('fa-IR-u-ca-persian', {
                timeZone: 'UTC', year: 'numeric', month: 'long'
            });
            var numberFormatter = new Intl.NumberFormat('fa-IR', { useGrouping: false });

            if (persianPartsFormatter.resolvedOptions().calendar !== 'persian')
                throw new Error('تقویم خورشیدی در مرورگر پشتیبانی نمی شود.');

            function numbersFromParts(formatter, date) {
                var output = {};
                formatter.formatToParts(date).forEach(function (piece) {
                    if (['year', 'month', 'day'].indexOf(piece.type) !== -1)
                        output[piece.type] = Number(piece.value);
                });
                return output;
            }

            function persianDay(epochDay) {
                return numbersFromParts(persianPartsFormatter, new Date(epochDay));
            }

            // روز میلادی متناظر با امروز تهران در UTC نرمال می شود.
            // این روش برای ساعت های نزدیک نیمه شب نیز مستقل از ساعت سیستم کاربر است.
            function todayEpoch() {
                var day = numbersFromParts(tehranGregorian, new Date());
                return Date.UTC(day.year, day.month - 1, day.day);
            }

            function monthStart(epochDay) {
                return epochDay - (persianDay(epochDay).day - 1) * DAY;
            }

            function nextMonthStart(epochStart) {
                var current = persianDay(epochStart);
                var candidate = epochStart + 28 * DAY;
                for (var attempt = 0; attempt < 5; attempt++) {
                    var candidatePersian = persianDay(candidate);
                    if (candidatePersian.month !== current.month || candidatePersian.year !== current.year)
                        return candidate;
                    candidate += DAY;
                }
                throw new Error('تعداد روزهای ماه قابل تشخیص نیست.');
            }

            function renderCalendar() {
                if (visibleMonthStart === null) return;

                var today = todayEpoch();
                var start = visibleMonthStart;
                var end = nextMonthStart(start);
                var dayCount = Math.round((end - start) / DAY);
                var weekdayOffset = (new Date(start).getUTCDay() + 1) % 7;

                monthTitle.text(monthFormatter.format(new Date(start)));
                daysGrid.empty();

                for (var empty = 0; empty < weekdayOffset; empty++) {
                    $('<span aria-hidden="true"></span>').appendTo(daysGrid);
                }

                for (var day = 1; day <= dayCount; day++) {
                    var epoch = start + (day - 1) * DAY;
                    var cell = $('<span role="gridcell"></span>')
                        .text(numberFormatter.format(day))
                        .attr('aria-label', 'روز ' + numberFormatter.format(day))
                        .appendTo(daysGrid);

                    if (new Date(epoch).getUTCDay() === 5) cell.addClass('is-friday');
                    if (epoch === today) {
                        cell.addClass('is-today').attr('aria-current', 'date')
                            .attr('title', 'امروز');
                    }
                }
            }

            function closeCalendar() {
                calendar.prop('hidden', true);
                trigger.attr('aria-expanded', 'false');
            }

            function showCalendar() {
                visibleMonthStart = monthStart(todayEpoch());
                renderCalendar();
                calendar.prop('hidden', false);
                trigger.attr('aria-expanded', 'true');
            }

            function updateClock() {
                var now = new Date();
                dateText.text(dateFormatter.format(now));
                timeText.text(timeFormatter.format(now));

                var currentDay = todayEpoch();
                if (lastLocalDay !== currentDay) {
                    lastLocalDay = currentDay;
                    if (!calendar.prop('hidden')) renderCalendar();
                }
            }

            trigger.on('click', function () {
                if (calendar.prop('hidden')) showCalendar();
                else closeCalendar();
            });

            $('#tirIranPreviousMonth').on('click', function () {
                var previousDay = visibleMonthStart - DAY;
                visibleMonthStart = monthStart(previousDay);
                renderCalendar();
            });

            $('#tirIranNextMonth').on('click', function () {
                visibleMonthStart = nextMonthStart(visibleMonthStart);
                renderCalendar();
            });

            $('#tirIranGoToToday').on('click', function () {
                visibleMonthStart = monthStart(todayEpoch());
                renderCalendar();
            });

            $('#tirIranCloseCalendar').on('click', function () {
                closeCalendar();
                trigger.trigger('focus');
            });

            $(document).on('pointerdown.tirIranClock', function (event) {
                if (!$(event.target).closest('#tirIranClock').length)
                    closeCalendar();
            });

            $(document).on('keydown.tirIranClock', function (event) {
                if (event.key === 'Escape' && !calendar.prop('hidden')) {
                    closeCalendar();
                    trigger.trigger('focus');
                }
            });

            $(document).on('visibilitychange.tirIranClock', function () {
                if (!document.hidden) updateClock();
            });

            updateClock();
            window.setInterval(updateClock, 1000);
        } catch (error) {
            dateText.text('تقویم خورشیدی در دسترس نیست');
            timeText.text('--:--:--');
            trigger.prop('disabled', true);
            if (window.console && console.error) console.error(error);
        }
    });
})(jQuery);
