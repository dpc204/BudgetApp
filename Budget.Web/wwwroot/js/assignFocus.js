// Focus management for Assign page
window.lastClickedRowIndex = -1;

// Track which row is being edited
document.addEventListener('click', function(e) {
  const cell = e.target.closest('td');
  if (cell) {
    const row = cell.closest('tr.mud-table-row');
    if (row) {
      const tbody = row.closest('.mud-table-body');
      if (tbody) {
        const allRows = tbody.querySelectorAll('.mud-table-row');
        const rowIndex = Array.from(allRows).indexOf(row);
        if (rowIndex >= 0) {
          window.lastClickedRowIndex = rowIndex;
        }
      }
    }
  }
});

window.setNotesColumnFocus = function (rowIndex, retryCount = 0, maxRetries = 20) {
  // Use the last clicked row if rowIndex is -1
  if (rowIndex === -1 && window.lastClickedRowIndex >= 0) {
    rowIndex = window.lastClickedRowIndex;
  }

  const grid = document.querySelector('.envelopes-table');
  if (!grid) {
    return;
  }

  const rows = grid.querySelectorAll('.mud-table-body .mud-table-row');

  // If no rows found and we haven't exceeded retries, wait and try again
  if (rows.length === 0 && retryCount < maxRetries) {
    setTimeout(() => {
      window.setNotesColumnFocus(rowIndex, retryCount + 1, maxRetries);
    }, 100);
    return;
  }

  if (!rows || rowIndex >= rows.length) {
    return;
  }

  const targetRow = rows[rowIndex];
  const notesCellElement = targetRow.querySelector('td[data-label="Notes"]');

  if (!notesCellElement) {
    return;
  }

  // Check if cell already has an input (already in edit mode)
  let input = notesCellElement.querySelector('input, textarea, .mud-input-slot input, .mud-input-slot textarea');

  if (input) {
    input.focus();
    return;
  }

  // Cell not in edit mode, click to enter edit mode
  notesCellElement.click();

  // Wait for MudDataGrid to enter edit mode and render the input
  setTimeout(() => {
    input = notesCellElement.querySelector('input, textarea, .mud-input-slot input, .mud-input-slot textarea');
    if (input) {
      input.focus();
    }
  }, 100);
};
