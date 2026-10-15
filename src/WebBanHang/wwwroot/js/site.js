document.querySelectorAll('form[data-confirm]').forEach(form => {
  form.addEventListener('submit', event => {
    if (!window.confirm(form.dataset.confirm)) event.preventDefault();
  });
});
const lines = document.querySelector('#order-lines');
function reindex() {
  lines?.querySelectorAll('.order-line').forEach((row, index) => {
    const select = row.querySelector('select');
    const input = row.querySelector('input');
    select.name = `Lines[${index}].ProductId`; select.id = `product-${index}`;
    input.name = `Lines[${index}].Quantity`; input.id = `quantity-${index}`;
    const labels = row.querySelectorAll('label');
    labels[0].htmlFor = select.id; labels[1].htmlFor = input.id;
    row.querySelector('.remove-line').disabled = lines.children.length === 1;
  });
}
document.querySelector('#add-line')?.addEventListener('click', () => {
  if (lines.children.length >= 50) return;
  const row = lines.firstElementChild.cloneNode(true);
  row.querySelector('select').value = ''; row.querySelector('input').value = 1;
  lines.append(row); reindex(); row.querySelector('select').focus();
});
lines?.addEventListener('click', event => {
  if (event.target.matches('.remove-line') && lines.children.length > 1) {
    event.target.closest('.order-line').remove(); reindex();
  }
});
reindex();
