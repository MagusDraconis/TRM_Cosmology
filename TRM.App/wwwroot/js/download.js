window.trmDownloadFile = (fileName, contentType, contentBase64) => {
    const link = document.createElement('a');
    link.download = fileName;
    link.href = `data:${contentType};base64,${contentBase64}`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};
