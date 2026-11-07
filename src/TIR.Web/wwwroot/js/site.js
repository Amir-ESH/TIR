(function ($) {
    'use strict';

    $(function () {
        var path = window.location.pathname.toLowerCase().replace(/\/+$/, '') || '/';

        var activeLink = null;
        var bestMatchLength = -1;

        $('.side-nav a').removeClass('active').each(function () {

            var href = ($(this).attr('href') || '')
                .toLowerCase()
                .replace(/\/+$/, '') || '/';

            // اگر آدرس دقیق پیدا شد همان منو انتخاب شود.
            if (href === path) {
                activeLink = this;
                return false;
            }

            // اگر آدرس دقیق نبود نزدیک ترین مسیر والد انتخاب شود.
            if (
                href !== '/' &&
                href !== '/hangfire' &&
                path.startsWith(href + '/') &&
                href.length > bestMatchLength
            ) {
                activeLink = this;
                bestMatchLength = href.length;
            }
        });

        // فقط یک منو فعال باشد.
        if (activeLink) {
            $(activeLink).addClass('active');
        }

        $('#menuToggle').on('click', function () { $('#appSidebar').toggleClass('open'); });
        $(document).on('click', function (event) {
            if (!$(event.target).closest('#appSidebar,#menuToggle').length)
                $('#appSidebar').removeClass('open');
        });

        // فرم های داخل جدول های داینامیک نیز باید تایید حذف و بازیابی داشته باشند.
        $(document).on('submit', 'form[data-confirm]', function (event) {
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
                    // پس از ذخیره مستقل، مقدار مبنای تشخیص تغییرات فرم اصلاح می شود.
                    form.trigger('tir:field-saved');
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

    // ورودی های تاریخ، تاریخ و ساعت و ساعت از انتخابگرهای مشترک استفاده می کنند.
    function initializePersianPicker() {
        if (!window.TirJalaliNavigation || !window.TirTimePicker) return;

        var popup = $('<div class="jalali-popup shadow"></div>').hide().appendTo('body');
        var timePicker = window.TirTimePicker.createPicker();
        var input = null;

        var calendar = window.TirJalaliNavigation.createCalendar(popup, {
            onSelectDay: function (year, month, day) {
                if (!input) return;
                var target = input;
                var selectedDate = [String(year).padStart(4, '0'),
                    String(month).padStart(2, '0'),
                    String(day).padStart(2, '0')].join('/');

                popup.hide();
                if (target.hasClass('js-jalali-datetime')) {
                    // ابتدا تاریخ و سپس ساعت انتخاب می شود؛ لغو ساعت مقدار قبلی را حفظ می کند.
                    var parts = target.val().toString().trim().split(/\s+/);
                    timePicker.open(target, {
                        value: parts.length > 1 ? parts[1] : '',
                        onSelect: function (selectedTime) {
                            target.val(selectedDate + ' ' + selectedTime).trigger('change');
                        }
                    });
                } else {
                    target.val(selectedDate).trigger('change');
                }
            }
        });

        $(document).on('focus', '.js-jalali-date,.js-jalali-datetime', function () {
            input = $(this);
            timePicker.close();

            var raw = input.val().toString().replace(/[۰-۹٠-٩]/g, function (digit) {
                var code = digit.charCodeAt(0);
                return String.fromCharCode(48 + code - (code >= 0x06f0 ? 0x06f0 : 0x0660));
            });
            var date = raw.match(/(\d{4})\/(\d{1,2})\/(\d{1,2})/);
            if (date) calendar.setDate(Number(date[1]), Number(date[2]), Number(date[3]));
            else calendar.resetToToday();

            var position = input.offset();
            popup.css({
                position: 'absolute',
                top: position.top + input.outerHeight() + 5,
                left: position.left,
                zIndex: 1060
            }).show();
            var maxLeft = Math.max(10, $(window).width() - popup.outerWidth() - 10);
            popup.css('left', Math.min(Math.max(10, position.left), maxLeft));
        });

        // کلیک روی هر نقطه فیلد ساعت، انتخابگر را باز می کند.
        $(document).on('focus click', '.js-tir-time', function () {
            var target = $(this);
            if (timePicker.isOpenFor(target)) return;

            popup.hide();
            timePicker.open(target, {
                value: target.val(),
                onSelect: function (selectedTime) {
                    target.val(selectedTime).trigger('change');
                }
            });
        });

        $(document).on('mousedown', function (event) {
            if (!$(event.target).closest('.jalali-popup,.js-jalali-date,.js-jalali-datetime').length)
                popup.hide();
        });

        $(document).on('keydown', function (event) {
            if (event.key === 'Escape' && popup.is(':visible')) popup.hide();
        });
    }
})(jQuery);
