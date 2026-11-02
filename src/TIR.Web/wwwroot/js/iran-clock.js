(function ($) {
    'use strict';

    $(function () {
        if (!$('#tirIranClock').length) return;

        var dateText = $('#tirIranToday');
        var timeText = $('#tirIranTime');
        var trigger = $('#tirIranDateTrigger');
        var popup = $('#tirIranCalendar');
        var lastLocalDay = null;

        try {
            // نمایش تاریخ و ساعت تهران با تقویم مشترک برنامه هماهنگ است.
            if (!window.TirJalaliNavigation)
                throw new Error('تقویم خورشیدی بارگذاری نشده است.');

            var dateFormatter = new Intl.DateTimeFormat('fa-IR-u-ca-persian', {
                timeZone: 'Asia/Tehran', weekday: 'long',
                year: 'numeric', month: 'long', day: 'numeric'
            });
            var timeFormatter = new Intl.DateTimeFormat('fa-IR', {
                timeZone: 'Asia/Tehran', hour: '2-digit', minute: '2-digit',
                second: '2-digit', hourCycle: 'h23'
            });
            var calendar = window.TirJalaliNavigation.createCalendar($('#tirIranCalendarContent'));

            function closeCalendar() {
                popup.prop('hidden', true);
                trigger.attr('aria-expanded', 'false');
            }

            function updateClock() {
                var now = new Date();
                dateText.text(dateFormatter.format(now));
                timeText.text(timeFormatter.format(now));

                var today = window.TirJalaliNavigation.todayParts();
                var dateKey = today.year + '/' + today.month + '/' + today.day;
                if (lastLocalDay !== dateKey) {
                    lastLocalDay = dateKey;
                    if (!popup.prop('hidden')) calendar.refresh();
                }
            }

            trigger.on('click', function () {
                if (!popup.prop('hidden')) {
                    closeCalendar();
                    return;
                }
                calendar.resetToToday();
                popup.prop('hidden', false);
                trigger.attr('aria-expanded', 'true');
            });

            $('#tirIranGoToToday').on('click', function () { calendar.resetToToday(); });

            $('#tirIranCloseCalendar').on('click', function () {
                closeCalendar();
                trigger.trigger('focus');
            });

            $(document).on('pointerdown.tirIranClock', function (event) {
                if (!$(event.target).closest('#tirIranClock').length) closeCalendar();
            });

            $(document).on('keydown.tirIranClock', function (event) {
                if (event.key === 'Escape' && !popup.prop('hidden')) {
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
