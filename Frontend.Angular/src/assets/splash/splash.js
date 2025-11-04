// Fallback loader timeout (safety mechanism)
setTimeout(() => {
  const loader = document.getElementById('app-init-loader');
  if (loader && !loader.classList.contains('app-ready')) {
    loader.innerHTML = `
      <div style="text-align:center;color:white;font-size:1rem;padding:2rem;">
        ⚠️ Application failed to start.<br/>
        <button onclick="location.reload()" style="
          margin-top:1rem;background:white;color:#4f46e5;
          border:none;padding:0.5rem 1rem;border-radius:0.4rem;
          cursor:pointer;font-weight:600;">Reload</button>
      </div>`;
  }
}, 30000);
