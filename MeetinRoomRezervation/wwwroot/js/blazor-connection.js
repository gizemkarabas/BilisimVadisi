// Blazor SignalR bağlantı yönetimi
window.blazorCulture = {
  get: () => window.localStorage["BlazorCulture"],
  set: (value) => (window.localStorage["BlazorCulture"] = value),
};

// Blazor bağlantı durumunu izle
Blazor.start({
  reconnectionOptions: {
    maxRetries: 5,
    retryIntervalMilliseconds: 2000,
  },
})
  .then(() => {
    console.log("Blazor started successfully");
  })
  .catch((err) => {
    console.error("Blazor start failed:", err);

    // Kullanıcıya hata bildirimi göster
    if (document.querySelector(".connection-error-banner") === null) {
      const banner = document.createElement("div");
      banner.className = "connection-error-banner";
      banner.innerHTML = `
            <div style="background-color: #f8d7da; color: #721c24; padding: 10px; text-align: center; border: 1px solid #f5c6cb;">
                <strong>Bağlantı Hatası:</strong> Sunucu ile bağlantı kurulamadı. Sayfayı yenilemeyi deneyin.
                <button onclick="location.reload()" style="margin-left: 10px; padding: 5px 10px;">
                    Yenile
                </button>
            </div>
        `;
      document.body.insertBefore(banner, document.body.firstChild);
    }
  });

// Bağlantı durumu değişikliklerini izle
window.addEventListener("beforeunload", () => {
  console.log("Page unloading, Blazor connection will be closed");
});
