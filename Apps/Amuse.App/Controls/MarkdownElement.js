document.addEventListener('click', function(e) {
    let content = '';
    const codecopy = e.target.closest('.copy-code');
    const thinking = e.target.id === 'thinking-summary';
    if(thinking || codecopy)
        e.preventDefault();
    if (codecopy) {
        content = codecopy.closest('.copy-block')?.querySelector('.copy-content')?.textContent ?? '';
        codecopy.textContent = '✓';
        setTimeout(() => {
            codecopy.textContent = '📋';
        }, 1200);
    }
    window.chrome.webview.postMessage({
        Type: thinking ? 'Thinking' : 'Click',
        X: e.clientX,
        Y: e.clientY,
        Clipboard: content
    });
}, true);

const sendSize = () => {
    chrome.webview.postMessage({
        Type: 'Resize',
        X: document.body.scrollWidth,
        Y: document.body.scrollHeight
    });
};

const observer = new ResizeObserver(sendSize);
observer.observe(document.documentElement);
sendSize();