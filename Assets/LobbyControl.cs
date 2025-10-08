using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Mirror;
// using Mirror.Transports.Relay;
using TMPro;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.UI;

public class LobbyControl : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button findMatchButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject matchmakingUI;

    private Lobby joinedLobby;
    private const string RelayJoinCodeKey = "RelayJoinCode";

    // Coroutine'i durdurabilmek için referansını tutuyoruz
    private Coroutine heartbeatCoroutine;

    async void Start()
    {
        // NetworkManager'dan RelayTransport'u bul
        // if (relayTransport == null)
        // {
        //     Debug.LogError("RelayTransport bulunamadı!");
        //     statusText.text = "Hata: Relay Transport eksik!";
        //     return;
        // }

        statusText.text = "Servisler Başlatılıyor...";
        findMatchButton.interactable = false;

        // Unity servislerini başlat ve anonim olarak giriş yap
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        statusText.text = "Maça Hazır!";
        findMatchButton.interactable = true;
    }

    public async void FindMatch()
    {
        statusText.text = "Maç Aranıyor...";
        findMatchButton.interactable = false;

        try
        {
            // İçinde boş yer olan (1 kişilik) bir lobi ara
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = 1,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "1", QueryFilter.OpOptions.EQ)
                }
            };

            QueryResponse lobbies = await LobbyService.Instance.QueryLobbiesAsync(options);

            if (lobbies.Results.Count > 0)
            {
                // ### CLIENT YOLU: Lobi bulundu, katıl ###
                Debug.Log("Lobi bulundu, katılınyor...");
                Lobby foundLobby = lobbies.Results[0];

                joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(foundLobby.Id);

                // Lobinin public datasından Relay Join Kodunu al
                string relayJoinCode = joinedLobby.Data[RelayJoinCodeKey].Value;

                // Relay'e bu kod ile Client olarak bağlan
                // await Relay.ClientRelay.JoinAllocationAsync(relayJoinCode);
                
                NetworkManager.singleton.StartClient();
                statusText.text = "Maça Katılınıldı!";
            }
            else
            {
                // ### HOST YOLU: Lobi bulunamadı, yeni bir tane oluştur ###
                Debug.Log("Lobi bulunamadı, yenisi oluşturuluyor...");
                
                // Host olacağımız için önce Relay'i oluşturup Join Kodunu almalıyız
                var allocation = await RelayService.Instance.CreateAllocationAsync(1); // Kendisi hariç 1 client
                string newJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                // Lobi oluşturma ayarları
                CreateLobbyOptions lobbyOptions = new CreateLobbyOptions
                {
                    // IsPublic = true,
                    // Join Kodunu lobinin datasına ekliyoruz ki Client'lar görebilsin
                    Data = new Dictionary<string, DataObject>
                    {
                        { RelayJoinCodeKey, new DataObject(DataObject.VisibilityOptions.Member, newJoinCode) }
                    }
                };
                
                // 2 kişilik yeni lobi oluştur
                joinedLobby = await LobbyService.Instance.CreateLobbyAsync("Yeni Mac", 2, lobbyOptions);

                // Lobiyi UGS sunucularında canlı tutmak için periyodik ping gönder
                heartbeatCoroutine = StartCoroutine(HeartbeatLobby(joinedLobby.Id, 15f));

                // Host olarak Relay'i ve Mirror'ı başlat
                // relayTransport.serverRelayData = new RelayServerData(allocation, "dtls");
                NetworkManager.singleton.StartHost();
                
                statusText.text = "Lobi Oluşturuldu, Rakip Bekleniyor...";
            }
            
            // Eşleşme tamamlanınca veya lobi kurulunca UI'ı kapat
            matchmakingUI.SetActive(false);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Lobi Hatası: {e}");
            statusText.text = "Eşleştirme Başarısız Oldu.";
            findMatchButton.interactable = true;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Relay Hatası: {e}");
            statusText.text = "Bağlantı Kurulamadı.";
            findMatchButton.interactable = true;
        }
    }

    // Host'un lobiyi UGS'e "ben hala buradayım" diye bildirmesi için
    private System.Collections.IEnumerator HeartbeatLobby(string lobbyId, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);
        while (true)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }

    private async void LeaveLobby()
    {
        // Eğer bir lobiye bağlıysak ve oyundan çıkıyorsak lobiyi temizle
        if (joinedLobby != null)
        {
            // Coroutine'i durdur ki arkada çalışmasın
            if (heartbeatCoroutine != null) StopCoroutine(heartbeatCoroutine);

            string playerId = AuthenticationService.Instance.PlayerId;

            // Eğer biz Host isek lobiyi tamamen sil
            if (joinedLobby.HostId == playerId)
            {
                Debug.Log("Lobi Host tarafından siliniyor...");
                await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
            }
            // Değilsek sadece lobiden ayrıl
            else
            {
                Debug.Log("Oyuncu lobiden ayrılıyor...");
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
            }
            joinedLobby = null;
        }
    }

    // Network bağlantısı koptuğunda veya durduğunda lobiyi terk et
    private void OnDisable()
    {
        // NetworkManager.singleton.isNetworkActive gibi bir kontrol de eklenebilir
        LeaveLobby();
    }
}