using Report.API.DTOs;

namespace Report.API.Services
{
    public class ContactApiClient
    {
        private readonly HttpClient _httpClient;

        public ContactApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<HttpResponseMessage> GetAllPersonsAsync()
        {
            return await _httpClient.GetAsync("/api/persons");
        }

        public async Task<HttpResponseMessage> GetPersonAsync(Guid id)
        {
            return await _httpClient.GetAsync($"/api/persons/{id}");
        }

        public async Task<HttpResponseMessage> CreatePersonAsync(PersonCreateDto dto)
        {
            return await _httpClient.PostAsJsonAsync("/api/persons", dto);
        }

        public async Task<HttpResponseMessage> DeletePersonAsync(Guid id)
        {
            return await _httpClient.DeleteAsync($"/api/persons/{id}");
        }

        public async Task<HttpResponseMessage> AddContactInfoAsync(Guid personId, ContactInfoCreateDto dto)
        {
            return await _httpClient.PostAsJsonAsync($"/api/persons/{personId}/ContactInfos", dto);
        }

        public async Task<HttpResponseMessage> DeleteContactInfoAsync(Guid personId, Guid contactId)
        {
            return await _httpClient.DeleteAsync($"/api/persons/{personId}/ContactInfos/{contactId}");
        }

        public async Task<HttpResponseMessage> RequestReportAsync(Guid personId)
        {
            return await _httpClient.PostAsync($"/api/persons/{personId}/request-report", null);
        }
    }

}
