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

    // ورودی های تاریخ و تاریخ و ساعت از تقویم مشترک استفاده می کنند.
    function initializePersianPicker() {
        if (!window.TirJalaliNavigation) return;

        var popup = $('<div class="jalali-popup shadow"></div>').hide().appendTo('body');
        var input = null;
        var calendar = window.TirJalaliNavigation.createCalendar(popup, {
            onSelectDay: function (year, month, day) {
                if (!input) return;

                var value = [String(year).padStart(4, '0'),
                    String(month).padStart(2, '0'),
                    String(day).padStart(2, '0')].join('/');

                if (input.hasClass('js-jalali-datetime')) {
                    // انتخاب روز، ساعت ثبت شده را تغییر نمی دهد.
                    var previous = input.val().toString().trim().split(/\s+/);
                    input.val(value + ' ' + (previous.length > 1 ? previous[1] : '12:00'));
                } else {
                    input.val(value);
                }
                popup.hide();
                input.trigger('change');
            }
        });

        $(document).on('focus', '.js-jalali-date,.js-jalali-datetime', function () {
            input = $(this);
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
                left: position.left, zIndex: 1060
            }).show();
            // عرض تقویم برای نمایش در صفحه کوچک تنظیم می شود.
            var maxLeft = Math.max(10, $(window).width() - popup.outerWidth() - 10);
            popup.css('left', Math.min(Math.max(10, position.left), maxLeft));
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
