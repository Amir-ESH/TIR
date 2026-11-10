(function ($) {
    'use strict';
    function setup(settings) {
        var attributes = JSON.parse($(settings.attributeData).text() || '[]');
        var operators = [
            ['eq', 'مساوی'],
            ['ne', 'نامساوی'],
            ['gt', 'بزرگ تر'],
            ['gte', 'بزرگ تر یا مساوی'],
            ['lt', 'کوچک تر'],
            ['lte', 'کوچک تر یا مساوی'],
            ['between', 'بین'],
            ['contains', 'شامل متن'],
            ['isnull', 'مقدار ندارد'],
            ['isnotnull', 'مقدار دارد']
        ];

        var container = $(settings.container);
        var optionCache = {};
        var optionsUrl = $(settings.attributeData).data('options-url');

        function createRow(condition) {
            if (container.children().length >= 64) {
                window.TIRMessages.warning('حداکثر 64 شرط مجاز است.');

                return;
            }

            var row = $('<div class="query-row"></div>');
            $('<input type="number" min="1" max="16" class="form-control condition-group" title="شماره گروه OR" />')
                .val(condition.group || 1).appendTo(row);

            var attribute = $('<select class="form-select condition-attribute"></select>').appendTo(row);

            $('<option value="">انتخاب مشخصه</option>').appendTo(attribute);

            attributes.forEach(function (item) {
                $('<option></option>').val(item.id).text(item.title)
                    .attr('data-kind', item.kind).attr('data-required', item.required ? 'true' : 'false')
                    .appendTo(attribute);
            });

            var operator = $('<select class="form-select condition-operator"></select>').appendTo(row);
            operators.forEach(function (item) { $('<option></option>').val(item[0]).text(item[1]).appendTo(operator); });
            $('<input class="form-control condition-value" placeholder="مقدار" />').val(condition.value || '').appendTo(row);
            $('<input class="form-control to-value condition-to" placeholder="تا مقدار" />').val(condition.to || '').appendTo(row);
            $('<button type="button" class="btn btn-outline-danger js-remove-condition" title="حذف شرط">×</button>').appendTo(row);
            container.append(row);

            if (condition.attributeId)
                attribute.val(condition.attributeId.toString());

            if (condition.operator)
                operator.val(condition.operator);

            updateRow(row);
        }

        function updateRow(row) {
            var kind = Number(row.find('.condition-attribute option:selected').data('kind'));
            var selectedAttribute = Number(row.find('.condition-attribute').val());
            var isRequired = row.find('.condition-attribute option:selected').attr('data-required') === 'true';
            var operator = row.find('.condition-operator');

            operator.find('option').each(function () {
                var code = $(this).val();
                var allow = code === 'eq' || code === 'ne' ||
                    (code === 'contains' && kind === 1) ||
                    (['gt', 'gte', 'lt', 'lte', 'between'].includes(code) && [2, 3, 4, 5, 6].includes(kind)) ||
                    (['isnull', 'isnotnull'].includes(code) && selectedAttribute > 0 && !isRequired);
                $(this).prop('disabled', !allow);
            });

            if (operator.find('option:selected').prop('disabled'))
                operator.val('eq');

            var isValuePresenceFilter = ['isnull', 'isnotnull'].includes(operator.val());
            row.toggleClass('is-between', operator.val() === 'between');
            var valueField = row.find('.condition-value');

            if (kind === 8 && !valueField.is('select')) {
                var priorValue = valueField.val();
                valueField.replaceWith($('<select class="form-select condition-value"><option value="">در حال بارگذاری...</option></select>'));
                valueField = row.find('.condition-value');
                valueField.data('prior', priorValue);
            }
            else if (kind !== 8 && valueField.is('select')) {
                valueField.replaceWith($('<input class="form-control condition-value" placeholder="مقدار" />'));
                valueField = row.find('.condition-value');
            }

            if (kind === 8 && selectedAttribute && !isValuePresenceFilter) {
                var wanted = valueField.data('prior') || valueField.val();
                var fillOptions = function (options) {
                    if (Number(row.find('.condition-attribute').val()) !== selectedAttribute) return;

                    var select = row.find('.condition-value').empty();
                    $('<option value="">انتخاب گزینه</option>').appendTo(select);
                    options.forEach(function (item) {
                        $('<option></option>').val(item.id).text(item.title).appendTo(select);
                    });

                    select.val(wanted);
                    select.removeData('prior');
                };

                if (optionCache[selectedAttribute]) {
                    fillOptions(optionCache[selectedAttribute]);
                }
                else {
                    $.getJSON(optionsUrl, { attributeId: selectedAttribute })
                        .done(function (options) {
                            optionCache[selectedAttribute] = options;
                            fillOptions(options);
                        })
                        .fail(function () {
                            row.find('.condition-value').empty().append('<option value="">بارگذاری گزینه ها ناموفق بود</option>');
                        });
                }
            }

            row.find('.condition-value,.condition-to').removeClass('js-jalali-date js-jalali-datetime js-tir-time');

            if (kind === 4)
                row.find('.condition-value,.condition-to').addClass('js-jalali-date');

            if (kind === 6)
                row.find('.condition-value,.condition-to').addClass('js-jalali-datetime');

            if (kind === 5)
                row.find('.condition-value,.condition-to').addClass('js-tir-time');

            var placeholder = kind === 4 ? '1405/08/01' : kind === 6 ? '1405/08/01 07:00' :
                kind === 5 ? 'HH:mm' :
                    kind === 7 ? '0 یا 1' : kind === 8 ? 'شناسه گزینه' : 'مقدار';

            row.find('.condition-value').attr('placeholder', placeholder);

            row.find('.condition-value,.condition-to')
                .prop('disabled', isValuePresenceFilter)
                .toggleClass('d-none', isValuePresenceFilter);
        }

        $(settings.addCondition).on('click', function () {
            createRow({ group: 1 });
        });

        container.on('click', '.js-remove-condition', function () {
            $(this).closest('.query-row').remove();
        });

        container.on('change', '.condition-operator,.condition-attribute', function () {
            updateRow($(this).closest('.query-row'));
        });

        function serializeConditions() {
            var conditions = [];
            container.children('.query-row').each(function () {
                var row = $(this), attr = Number(row.find('.condition-attribute').val());

                if (!attr) return;

                var isValuePresenceFilter = ['isnull', 'isnotnull'].includes(row.find('.condition-operator').val());

                conditions.push({
                    group: Number(row.find('.condition-group').val()),
                    attributeId: attr, operator: row.find('.condition-operator').val(),
                    value: isValuePresenceFilter ? null : String(row.find('.condition-value').val() || '').trim(),
                    to: isValuePresenceFilter ? null : String(row.find('.condition-to').val() || '').trim() || null });
            });

            $(settings.filtersJson).val(JSON.stringify(conditions));

            return conditions;
        }

        $(settings.form).on('submit', serializeConditions);

        $(document).on('click', settings.pagination || '.js-search-page', function () {
            $(settings.pageNumber).val($(this).data('page'));
            $(settings.form).trigger('submit');
        });

        var previous = [];

        try {
            previous = JSON.parse($(settings.filtersJson).val() || '[]');
        }
        catch (_) {
            previous = [];
        }

        if (previous.length) {
            previous.forEach(createRow);
        }
        else {
            createRow({ group: 1 });
        }

        var controller = { serialize: serializeConditions };
        window.TIRAdvancedSearch.active = controller;

        return controller;
    }

    window.TIRAdvancedSearch = { setup: setup };

    $(function () {
        if (!$('#advancedSearchForm').length) return;

        setup({
            form: '#advancedSearchForm',
            attributeData: '#attributeData',
            container: '#conditionsContainer',
            addCondition: '#addCondition',
            filtersJson: '#FiltersJson',
            pageNumber: '#PageNumber'
        });
    });
})(jQuery);
