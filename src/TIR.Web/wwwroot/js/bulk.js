(function ($) {
    'use strict';
    $(function () {
        var selection = 0;
        $('#CsvFile').on('change', function () {
            var currentSelection = ++selection;
            var file = this.files && this.files[0];
            if (!file) return;
            $('#bulkFileZone').addClass('has-file');
            var prefix = 'نام فایل: ' + file.name + ' | حجم: ' + (file.size / 1048576).toFixed(2) + ' مگابایت';
            $('#bulkFileInfo').text(prefix + ' | در حال شمارش کارمندان...');

            (async function () {
                if (!file.stream) { $('#bulkFileInfo').text(prefix); return; }
                var reader = file.stream().getReader(), decoder = new TextDecoder('utf-8');
                var pending = '', rows = 0, employees = 0, lastEmployee = null, invalidOrder = false;
                var handleLine = function (line) {
                    line = line.trim();
                    if (!line || line.charAt(0) === '#') return;
                    var employee = line.split(',', 1)[0].replace(/^\uFEFF/, '').trim();
                    if (employee.toLowerCase() === 'employeeid') return;
                    rows++;
                    if (employee !== lastEmployee) {
                        if (lastEmployee !== null && /^\d+$/.test(employee) && /^\d+$/.test(lastEmployee) &&
                            BigInt(employee) < BigInt(lastEmployee)) invalidOrder = true;
                        employees++;
                        lastEmployee = employee;
                    }
                };
                try {
                    while (true) {
                        var part = await reader.read();
                        if (currentSelection !== selection) { await reader.cancel(); return; }
                        if (part.done) break;
                        pending += decoder.decode(part.value, { stream: true });
                        var end;
                        while ((end = pending.indexOf('\n')) !== -1) {
                            handleLine(pending.substring(0, end));
                            pending = pending.substring(end + 1);
                        }
                    }
                    pending += decoder.decode();
                    if (pending.trim()) handleLine(pending);
                    if (currentSelection === selection)
                        $('#bulkFileInfo').text(prefix + ' | کارمندان هدف: ' + employees.toLocaleString('fa-IR') +
                            ' | ردیف ها: ' + rows.toLocaleString('fa-IR') +
                            (invalidOrder ? ' | هشدار: ترتیب شناسه کارمندان صحیح نیست.' : ''));
                } catch (error) {
                    if (currentSelection === selection) $('#bulkFileInfo').text(prefix + ' | شمارش اولیه ممکن نشد.');
                } finally { reader.releaseLock(); }
            })();
        });
        $('#bulkUploadForm').on('submit', function () {
            if (!this.checkValidity()) return;
            $('#startBulkButton').prop('disabled', true).text('در حال انتقال و پردازش...');
            $('<div class="busy-overlay show"><div class="busy-content"><div class="spinner-border text-primary mb-3"></div><p class="mb-0">لطفا تا دریافت نتیجه صبر کنید.</p></div></div>').appendTo('body');
        });
    });
})(jQuery);
