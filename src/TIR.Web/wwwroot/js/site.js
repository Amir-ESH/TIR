(function ($) {
    'use strict';

    $(function () {
        var path = window.location.pathname.toLowerCase();
        $('.side-nav a').each(function () {
            var href = ($(this).attr('href') || '').toLowerCase();
            if (href === path || (href !== '/' && href !== '/hangfire' && path.startsWith(href + '/')))
                $(this).addClass('active');
        });

        $('#menuToggle').on('click', function () { $('#appSidebar').toggleClass('open'); });
        $(document).on('click', function (event) {
            if (!$(event.target).closest('#appSidebar,#menuToggle').length)
                $('#appSidebar').removeClass('open');
        });

        $('form[data-confirm]').on('submit', function (event) {
            if (!window.confirm($(this).attr('data-confirm'))) event.preventDefault();
        });

        $('.js-filter-fields').on('input', function () {
            var query = $(this).val().toString().trim().toLowerCase();
            $(this).closest('.card').find('.field-grid > *').each(function () {
                $(this).toggle($(this).text().toLowerCase().includes(query));
            });
        });

        $(document).on('click', '.js-clear-field', function () {
            if (!window.confirm('این مقدار خالی شود؟')) return;
            var form = $(this).closest('form');
            form.find('.js-clear-value').val('true');
            form.trigger('submit');
        });

        $(document).on('submit', 'form[data-dynamic-save]', function (event) {
            event.preventDefault();
            var form = $(this), button = form.find('button[type=submit]');
            var message = $('#valueSaveMessage');
            button.prop('disabled', true).text('در حال ذخیره...');
            $.ajax({
                url: form.attr('action'), type: 'POST', data: form.serialize(),
                success: function (answer) {
                    if (!answer.success) { showMessage(answer.message, 'danger'); return; }
                    form.find('.js-rowversion').val(answer.rowVersion);
                    if (form.find('.js-clear-value').val() === 'true') {
                        form.find('[name="Field.RawValue"]').val('');
                        form.find('.js-bool-set,.js-bool-check').prop('checked', false);
                    }
                    showMessage('مقدار ذخیره شد. وضعیت فرمول های وابسته را در صفحه جزئیات بررسی کنید.', 'success');
                },
                error: function (response) {
                    var info = response.responseJSON || {};
                    showMessage(info.message || 'خطای ارتباط با سرور رخ داد.', 'danger');
                },
                complete: function () {
                    form.find('.js-clear-value').val('false');
                    button.prop('disabled', false).text('ذخیره این مقدار');
                }
            });

            function showMessage(text, kind) {
                message.html($('<div>').addClass('alert alert-' + kind).text(text));
                message[0].scrollIntoView({ behavior: 'smooth', block: 'nearest' });
            }
        });

        initializePersianPicker();
    });

    // انتخاب تاریخ در مرورگر انجام می شود؛ تبدیل مرجع نهایی در Backend است.
    function initializePersianPicker() {
        if (!window.Intl || !Intl.DateTimeFormat) return;
        var formatter = new Intl.DateTimeFormat('fa-IR-u-ca-persian-nu-latn',
            { year: 'numeric', month: 'numeric', day: 'numeric', timeZone: 'UTC' });
        var months = ['فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور',
            'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'];
        var weekdays = ['ش', 'ی', 'د', 'س', 'چ', 'پ', 'ج'];
        var popup = $('<div class="jalali-popup shadow"></div>').hide().appendTo('body');
        var input = null, currentYear, currentMonth;

        function parts(date) {
            var items = formatter.formatToParts(date), result = {};
            items.forEach(function (item) {
                if (item.type === 'year' || item.type === 'month' || item.type === 'day')
                    result[item.type] = Number(item.value);
            });
            return result;
        }

        function gregorianOf(year, month, day) {
            var initial = Date.UTC(year + 621, 2, 1);
            for (var offset = -45; offset < 425; offset++) {
                var date = new Date(initial + offset * 86400000), p = parts(date);
                if (p.year === year && p.month === month && p.day === day) return date;
            }
            return null;
        }

        function render() {
            popup.empty();
            var heading = $('<div class="jalali-heading"></div>').appendTo(popup);
            $('<button type="button" class="btn btn-sm btn-light">❮</button>').appendTo(heading)
                .on('click', function () { moveMonth(-1); });
            $('<strong></strong>').text(months[currentMonth - 1] + ' ' + currentYear).appendTo(heading);
            $('<button type="button" class="btn btn-sm btn-light">❯</button>').appendTo(heading)
                .on('click', function () { moveMonth(1); });
            var grid = $('<div class="jalali-calendar-grid"></div>').appendTo(popup);
            weekdays.forEach(function (name) { $('<span class="jalali-weekday"></span>').text(name).appendTo(grid); });
            var first = gregorianOf(currentYear, currentMonth, 1);
            if (!first) return;
            var nextMonth = currentMonth === 12 ? 1 : currentMonth + 1;
            var nextYear = currentMonth === 12 ? currentYear + 1 : currentYear;
            var next = gregorianOf(nextYear, nextMonth, 1);
            var dayCount = next ? Math.round((next - first) / 86400000) : 30;
            for (var empty = 0; empty < (first.getUTCDay() + 1) % 7; empty++)
                $('<span></span>').appendTo(grid);
            for (var day = 1; day <= dayCount; day++) {
                $('<button type="button" class="jalali-day"></button>').text(day).data('day', day)
                    .appendTo(grid).on('click', function () {
                        var selected = [currentYear.toString().padStart(4, '0'),
                            currentMonth.toString().padStart(2, '0'),
                            $(this).data('day').toString().padStart(2, '0')].join('/');
                        if (input.hasClass('js-jalali-datetime')) {
                            var prior = input.val().toString().split(' ');
                            input.val(selected + ' ' + (prior.length > 1 ? prior[1] : '12:00'));
                        } else input.val(selected);
                        popup.hide();
                        input.trigger('change');
                    });
            }
        }

        function moveMonth(direction) {
            currentMonth += direction;
            if (currentMonth < 1) { currentMonth = 12; currentYear--; }
            if (currentMonth > 12) { currentMonth = 1; currentYear++; }
            render();
        }

        $(document).on('focus', '.js-jalali-date,.js-jalali-datetime', function () {
            input = $(this);
            var match = input.val().toString().match(/(\d{4})\/(\d{1,2})\/(\d{1,2})/);
            var today = parts(new Date());
            currentYear = match ? Number(match[1]) : today.year;
            currentMonth = match ? Number(match[2]) : today.month;
            render();
            var pos = input.offset();
            popup.css({ top: pos.top + input.outerHeight() + 5, left: pos.left,
                position: 'absolute', zIndex: 1060 }).show();
        });
        $(document).on('mousedown', function (event) {
            if (!$(event.target).closest('.jalali-popup,.js-jalali-date,.js-jalali-datetime').length)
                popup.hide();
        });
    }
})(jQuery);
