// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Simple custom accordion logic for elements marked with data-sc-accordion
(function () {
    function initScAccordions() {
        var accordions = document.querySelectorAll('[data-sc-accordion="true"]');
        if (!accordions.length) return;

        accordions.forEach(function (accordion) {
            var single = accordion.getAttribute('data-sc-accordion-single') === 'true';

            accordion.querySelectorAll('[data-sc-accordion-toggle]').forEach(function (btn) {
                btn.addEventListener('click', function () {
                    var targetSelector = btn.getAttribute('data-sc-target');
                    if (!targetSelector) return;

                    var target = accordion.querySelector(targetSelector) || document.querySelector(targetSelector);
                    if (!target) return;

                    var isOpen = target.classList.contains('sc-open');

                    // Close other panels in this accordion (if single mode)
                    if (single) {
                        accordion.querySelectorAll('.sc-accordion-panel.sc-open').forEach(function (pane) {
                            if (pane === target) return;
                            pane.classList.remove('sc-open');
                            var headerBtn = accordion.querySelector('[data-sc-target="#' + pane.id + '"]');
                            if (headerBtn) {
                                headerBtn.classList.add('collapsed');
                            }
                        });
                    }

                    if (isOpen) {
                        target.classList.remove('sc-open');
                        btn.classList.add('collapsed');
                    } else {
                        target.classList.add('sc-open');
                        btn.classList.remove('collapsed');
                    }
                });
            });
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initScAccordions);
    } else {
        initScAccordions();
    }
})();

