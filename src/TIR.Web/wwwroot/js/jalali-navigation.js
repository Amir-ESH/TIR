(function ($) {
    'use strict';

    // تمام تقویم های هدر و ورودی ها از همین ماژول استفاده می کنند.
    var DAY = 86400000;
    var months = ['فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور',
        'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'];
    var weekdays = ['ش', 'ی', 'د', 'س', 'چ', 'پ', 'ج'];
    var yearsPerPage = 12;
    var numberFormatter = new Intl.NumberFormat('fa-IR', { useGrouping: false });
    var persianFormatter = new Intl.DateTimeFormat('fa-IR-u-ca-persian-nu-latn', {
        timeZone: 'UTC', year: 'numeric', month: 'numeric', day: 'numeric'
    });
    var tehranFormatter = new Intl.DateTimeFormat('en-US-u-nu-latn', {
        timeZone: 'Asia/Tehran', year: 'numeric', month: 'numeric', day: 'numeric'
    });

    // در نمایش راست به چپ، ماه قبل فلش راست و ماه بعد فلش چپ دارد.
    var previousIcon = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" ' +
        'width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" ' +
        'stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false">' +
        '<path d="m9 18 6-6-6-6"/></svg>';
    var nextIcon = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" ' +
        'width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" ' +
        'stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false">' +
        '<path d="m15 18-6-6 6-6"/></svg>';

    function parts(formatter, date) {
        var output = {};
        formatter.formatToParts(date).forEach(function (value) {
            if (value.type === 'year' || value.type === 'month' || value.type === 'day')
                output[value.type] = Number(value.value);
        });
        return output;
    }

    function persianDay(epoch) {
        return parts(persianFormatter, new Date(epoch));
    }

    function todayEpoch() {
        var date = parts(tehranFormatter, new Date());
        return Date.UTC(date.year, date.month - 1, date.day);
    }

    function todayParts() {
        return persianDay(todayEpoch());
    }

    function startOfMonth(epoch) {
        return epoch - (persianDay(epoch).day - 1) * DAY;
    }

    function nextMonthStart(start) {
        return startOfMonth(start + 32 * DAY);
    }

    function epochForMonth(year, month) {
        // بازه ماه با تقویم خورشیدی مرورگر محاسبه می شود.
        var start = startOfMonth(Date.UTC(year + 621, 2, 21) + (month - 1) * 30 * DAY);
        for (var i = 0; i < 5; i++) {
            var current = persianDay(start);
            if (current.year === year && current.month === month) return start;
            var difference = (year - current.year) * 12 + month - current.month;
            start = difference > 0 ? nextMonthStart(start) : startOfMonth(start - DAY);
        }
        throw new Error('ماه خورشیدی قابل تشخیص نیست.');
    }

    function yearPageStart(year) {
        return Math.max(1, Math.floor((year - 1) / yearsPerPage) * yearsPerPage + 1);
    }

    function addChoice(target, label, isSelected, callback) {
        $('<button type="button" class="jalali-choice"></button>')
            .text(label).toggleClass('is-selected', isSelected)
            .attr('aria-pressed', String(isSelected))
            .on('click', callback).appendTo(target);
    }

    function createCalendar(container, options) {
        options = options || {};
        var current = todayParts();
        var year = current.year, month = current.month;
        var selected = null, view = 'days', firstYear = yearPageStart(year);

        function setDate(newYear, newMonth, newDay) {
            year = Number(newYear);
            month = Number(newMonth);
            if (!Number.isInteger(year) || year < 1 || !Number.isInteger(month) ||
                month < 1 || month > 12) {
                var today = todayParts();
                year = today.year;
                month = today.month;
            }
            var day = Number(newDay);
            selected = Number.isInteger(day) && day > 0 && day <= 31
                ? { year: year, month: month, day: day } : null;
            view = 'days';
            firstYear = yearPageStart(year);
            render();
        }

        function move(direction) {
            if (view === 'years') firstYear = Math.max(1, firstYear + direction * yearsPerPage);
            else if (view === 'months') year = Math.max(1, year + direction);
            else if (direction < 0 && year === 1 && month === 1) return;
            else {
                month += direction;
                if (month < 1) { year--; month = 12; }
                if (month > 12) { year++; month = 1; }
            }
            render();
        }

        function periodButton(target, label, ariaLabel, callback) {
            $('<button type="button" class="jalali-period-button"></button>')
                .text(label).attr('aria-label', ariaLabel).on('click', callback)
                .appendTo(target);
        }

        function render() {
            container.empty();
            var header = $('<div class="tir-iran-calendar__heading"></div>').appendTo(container);
            var unit = view === 'years' ? '12 سال' : view === 'months' ? 'سال' : 'ماه';
            $('<button type="button" class="tir-iran-calendar__arrow"></button>')
                .html(previousIcon).attr('aria-label', unit + ' قبل')
                .attr('title', unit + ' قبل').on('click', function () { move(-1); })
                .appendTo(header);

            var center = $('<div class="tir-iran-calendar__period"></div>').appendTo(header);
            if (view === 'years') {
                $('<strong></strong>').text(numberFormatter.format(firstYear) + ' تا ' +
                    numberFormatter.format(firstYear + yearsPerPage - 1)).appendTo(center);
            } else {
                if (view === 'days') {
                    periodButton(center, months[month - 1], 'انتخاب ماه خورشیدی',
                        function () { view = 'months'; render(); });
                } else $('<strong>انتخاب ماه</strong>').appendTo(center);

                periodButton(center, numberFormatter.format(year), 'انتخاب سال خورشیدی',
                    function () { firstYear = yearPageStart(year); view = 'years'; render(); });
            }

            $('<button type="button" class="tir-iran-calendar__arrow"></button>')
                .html(nextIcon).attr('aria-label', unit + ' بعد')
                .attr('title', unit + ' بعد').on('click', function () { move(1); })
                .appendTo(header);

            var body = $('<div class="tir-iran-calendar__selection"></div>').appendTo(container);
            if (view === 'years') {
                body.addClass('jalali-choice-grid jalali-choice-grid--years');
                for (var valueYear = firstYear; valueYear < firstYear + yearsPerPage; valueYear++) {
                    (function (choice) {
                        addChoice(body, numberFormatter.format(choice), choice === year, function () {
                            year = choice;
                            view = 'months';
                            render();
                        });
                    })(valueYear);
                }
                return;
            }
            if (view === 'months') {
                body.addClass('jalali-choice-grid jalali-choice-grid--months');
                months.forEach(function (name, index) {
                    var choice = index + 1;
                    addChoice(body, name, choice === month, function () {
                        month = choice;
                        view = 'days';
                        render();
                    });
                });
                return;
            }

            var firstDay = epochForMonth(year, month);
            var dayCount = Math.round((nextMonthStart(firstDay) - firstDay) / DAY);
            var offset = (new Date(firstDay).getUTCDay() + 1) % 7;
            var todayEpochDay = todayEpoch();
            var weekdaysGrid = $('<div class="tir-iran-calendar__grid tir-iran-calendar__weekdays" aria-hidden="true"></div>')
                .appendTo(body);
            weekdays.forEach(function (label) {
                $('<span></span>').text(label).appendTo(weekdaysGrid);
            });

            var daysGrid = $('<div class="tir-iran-calendar__grid tir-iran-calendar__days" role="grid" aria-label="روزهای ماه"></div>')
                .appendTo(body);
            for (var empty = 0; empty < offset; empty++)
                $('<span aria-hidden="true"></span>').appendTo(daysGrid);

            for (var day = 1; day <= dayCount; day++) {
                (function (value) {
                    var epoch = firstDay + (value - 1) * DAY;
                    var isSelected = selected && selected.year === year &&
                        selected.month === month && selected.day === value;
                    var cell = $(options.onSelectDay
                        ? '<button type="button" class="tir-jalali-day" role="gridcell"></button>'
                        : '<span class="tir-jalali-day" role="gridcell"></span>');
                    cell.text(numberFormatter.format(value))
                        .attr('aria-label', 'روز ' + numberFormatter.format(value))
                        .toggleClass('is-friday', new Date(epoch).getUTCDay() === 5)
                        .toggleClass('is-today', epoch === todayEpochDay)
                        .toggleClass('is-selected', Boolean(isSelected))
                        .appendTo(daysGrid);

                    if (epoch === todayEpochDay) cell.attr('aria-current', 'date');
                    if (options.onSelectDay) {
                        cell.on('click', function () {
                            selected = { year: year, month: month, day: value };
                            options.onSelectDay(year, month, value);
                        });
                    }
                })(day);
            }
        }

        function resetToToday() {
            var today = todayParts();
            setDate(today.year, today.month, options.onSelectDay ? today.day : null);
        }

        resetToToday();
        return { setDate: setDate, resetToToday: resetToToday, refresh: render };
    }

    window.TirJalaliNavigation = {
        createCalendar: createCalendar,
        todayParts: todayParts
    };
})(jQuery);
