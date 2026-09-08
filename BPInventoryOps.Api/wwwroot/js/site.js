"use strict";
document.querySelectorAll("form[data-confirm]").forEach(form => {
    form.addEventListener("submit", event => {
        if (!window.confirm(form.dataset.confirm)) event.preventDefault();
    });
});
// Keep indexed form field names contiguous so Razor's collection binder receives every line.
const lines = document.querySelector("#restock-lines");
if (lines) {
    const reindex = () => {
        [...lines.children].forEach((line, index) => {
            line.querySelectorAll("[data-field]").forEach(input => {
                input.name = `Input.Items[${index}].${input.dataset.field}`;
                input.id = `line-${index}-${input.dataset.field}`;
                input.previousElementSibling.htmlFor = input.id;
            });
            line.querySelector(".remove-line").disabled = lines.children.length === 1;
            line.querySelector(".remove-line").hidden = false;
        });
    };
    document.querySelector("#add-line").addEventListener("click", event => {
        event.preventDefault();
        const copy = lines.firstElementChild.cloneNode(true);
        copy.querySelector("select").value = "";
        copy.querySelector("input").value = "1";
        lines.appendChild(copy);
        reindex();
        copy.querySelector("select").focus();
    });
    lines.addEventListener("click", event => {
        if (event.target.matches(".remove-line") && lines.children.length > 1) {
            event.target.closest(".restock-line").remove();
            reindex();
            document.querySelector("#add-line").focus();
        }
    });
    reindex();
}
