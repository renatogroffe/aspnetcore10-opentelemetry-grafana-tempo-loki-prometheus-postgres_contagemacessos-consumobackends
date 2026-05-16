using APIContagem.Models;

namespace APIContagem.Clients;

public class APIExternaClient
{
    public HttpClient _httpClient { get; }
    public IConfiguration _configuration { get; }

    public APIExternaClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<ResultadoAPIExterna?> ConsumirApiExternaAsync(bool java)
    {
        var url = java ? _configuration["UrlAPIExternaJava"] : _configuration["UrlAPIExternaNodeJs"];
        var response = await _httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            var resultado = await response.Content.ReadFromJsonAsync<ResultadoAPIExterna>();
            return resultado;
        }
        return null;
    }
}