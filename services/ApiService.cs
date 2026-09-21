using System.Net.Http.Json;
using lost_and_found.Models;

class ApiService
{
    private readonly HttpClient _httpClient;

    public ApiService()
    {
        this._httpClient = new HttpClient();
    }

    // public Task<LostFoundItem> getLostFound()
    // {
    //     return _httpClient.GetFromJsonAsync<
    // }
}