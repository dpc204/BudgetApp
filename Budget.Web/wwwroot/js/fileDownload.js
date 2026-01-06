// File download helper
export function downloadFile(fileName, contentType, base64Data) {
  const byteCharacters = atob(base64Data);
  const byteNumbers = new Array(byteCharacters.length);
  for (let i = 0; i < byteCharacters.length; i++) {
    byteNumbers[i] = byteCharacters.charCodeAt(i);
  }
  const byteArray = new Uint8Array(byteNumbers);
  const blob = new Blob([byteArray], { type: contentType });
  
  const link = document.createElement('a');
  link.href = URL.createObjectURL(blob);
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  
  // Clean up the blob URL after a reasonable delay
  setTimeout(() => URL.revokeObjectURL(link.href), 1000);
}

// Simpler download from data URL (for backward compatibility)
function downloadFileFromStream(fileName, dataUrl) {
  const anchorElement = document.createElement('a');
  anchorElement.href = dataUrl;
  anchorElement.download = fileName ?? '';
  anchorElement.click();
  anchorElement.remove();
}

export function initialize() {
  window.downloadFile = downloadFile;
  window.downloadFileFromStream = downloadFileFromStream;
}
