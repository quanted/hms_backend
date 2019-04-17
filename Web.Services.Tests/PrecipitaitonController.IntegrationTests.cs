﻿using Xunit;
using Web.Services.Controllers;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using System.Text;
using Data;
using System.Diagnostics;

namespace Web.Services.Tests
{
    /// <summary>
    /// Precipitation Controller Integration Test Class
    /// </summary>
    public class PrecipitaitonControllerIntegrationTests
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;

        /// <summary>
        /// NLDAS daily request json string for testing a valid request
        /// </summary>
        const string nldasRequest =
            "{\"source\": \"nldas\",\"dateTimeSpan\": {\"startDate\": \"2015-01-01T00:00:00\",\"endDate\": \"2015-12-31T00:00:00\"," +
            "\"dateTimeFormat\": \"yyyy-MM-dd HH\"},\"geometry\": {\"description\": \"EPA Athens Office\",\"point\": " +
            "{\"latitude\": 33.925673,\"longitude\": -83.355723},\"geometryMetadata\": {\"City\": \"Athens\",\"State\": \"Georgia\",\"Country\": \"United States\"}," +
            "\"timezone\": {\"name\": \"EST\",\"offset\": -5,\"dls\": false}},\"dataValueFormat\": \"E3\",\"temporalResolution\": \"default\",\"timeLocalized\": true," +
            "\"units\": \"default\",\"outputFormat\": \"json\"}";

        /// <summary>
        /// GLDAS daily request json string for testing a valid request
        /// </summary>
        const string gldasRequest =
            "{\"source\": \"gldas\",\"dateTimeSpan\": {\"startDate\": \"2015-01-01T00:00:00\",\"endDate\": \"2015-12-31T00:00:00\"," +
            "\"dateTimeFormat\": \"yyyy-MM-dd HH\"},\"geometry\": {\"description\": \"EPA Athens Office\",\"point\": " +
            "{\"latitude\": 33.925673,\"longitude\": -83.355723},\"geometryMetadata\": {\"City\": \"Athens\",\"State\": \"Georgia\",\"Country\": \"United States\"}," +
            "\"timezone\": {\"name\": \"EST\",\"offset\": -5,\"dls\": false}},\"dataValueFormat\": \"E3\",\"temporalResolution\": \"default\",\"timeLocalized\": true," +
            "\"units\": \"default\",\"outputFormat\": \"json\"}";

        /// <summary>
        /// DAYMET daily request json string for testing a valid request
        /// </summary>
        const string daymetRequest =
            "{\"source\": \"daymet\",\"dateTimeSpan\": {\"startDate\": \"2015-01-01T00:00:00\",\"endDate\": \"2015-12-31T00:00:00\"," +
            "\"dateTimeFormat\": \"yyyy-MM-dd HH\"},\"geometry\": {\"description\": \"EPA Athens Office\",\"point\": " +
            "{\"latitude\": 33.925673,\"longitude\": -83.355723},\"geometryMetadata\": {\"City\": \"Athens\",\"State\": \"Georgia\",\"Country\": \"United States\"}," +
            "\"timezone\": {\"name\": \"EST\",\"offset\": -5,\"dls\": false}},\"dataValueFormat\": \"E3\",\"temporalResolution\": \"default\",\"timeLocalized\": true," +
            "\"units\": \"default\",\"outputFormat\": \"json\"}";

        /// <summary>
        /// PRISM daily request json string for testing a valid request
        /// </summary>
        const string prismRequest =
            "{\"source\": \"prism\",\"dateTimeSpan\": {\"startDate\": \"2015-01-01T00:00:00\",\"endDate\": \"2015-12-31T00:00:00\"," +
            "\"dateTimeFormat\": \"yyyy-MM-dd HH\"},\"geometry\": {\"description\": \"EPA Athens Office\",\"point\": " +
            "{\"latitude\": 33.925673,\"longitude\": -83.355723},\"geometryMetadata\": {\"City\": \"Athens\",\"State\": \"Georgia\",\"Country\": \"United States\"}," +
            "\"timezone\": {\"name\": \"EST\",\"offset\": -5,\"dls\": false}},\"dataValueFormat\": \"E3\",\"temporalResolution\": \"default\",\"timeLocalized\": true," +
            "\"units\": \"default\",\"outputFormat\": \"json\"}";

        /// <summary>
        /// NCDC daily request json string for testing a valid request
        /// </summary>
        const string ncdcRequest =
             "{\"source\": \"ncdc\",\"dateTimeSpan\": {\"startDate\": \"2015-01-01T00:00:00\",\"endDate\": \"2015-12-31T00:00:00\"," +
            "\"dateTimeFormat\": \"yyyy-MM-dd HH\"},\"geometry\": {\"description\": \"EPA Athens Office\"," +
            "\"geometryMetadata\": {\"stationID\": \"GHCND:USW00013874\"}," +
            "\"timezone\": {\"name\": \"EST\",\"offset\": -5,\"dls\": false}},\"dataValueFormat\": \"E3\",\"temporalResolution\": \"default\",\"timeLocalized\": true," +
            "\"units\": \"default\",\"outputFormat\": \"json\"}";

        /// <summary>
        /// WGEN daily request json string for testing a valid request
        /// </summary>
        const string wgenRequest =
            "{\"source\": \"wgen\",\"dateTimeSpan\": {\"startDate\": \"2015-01-01T00:00:00\",\"endDate\": \"2015-12-31T00:00:00\"," +
            "\"dateTimeFormat\": \"yyyy-MM-dd HH\"},\"geometry\": {\"description\": \"EPA Athens Office\",\"point\": " +
            "{\"latitude\": 33.925673,\"longitude\": -83.355723},\"geometryMetadata\": {\"City\": \"Athens\",\"State\": \"Georgia\",\"Country\": \"United States\"}," +
            "\"timezone\": {\"name\": \"EST\",\"offset\": -5,\"dls\": false}},\"dataValueFormat\": \"E3\",\"temporalResolution\": \"default\",\"timeLocalized\": true," +
            "\"units\": \"default\",\"outputFormat\": \"json\"}";

        /// <summary>
        /// Integration test constructor creates test server and test client.
        /// </summary>
        public PrecipitaitonControllerIntegrationTests()
        {          
            _server = new TestServer(new WebHostBuilder().UseStartup<Startup>());
            _client = _server.CreateClient();
        }

        /// <summary>
        /// Precipitation controller integration tests for each daily precip data source. All tests should pass.
        /// Test Exception: precip test for prism may fail due to firewall restrictions.
        /// Daily temporal resolution chosen as it's the mininum resolution shared by all datasets
        /// </summary>
        /// <param name="precipInputString"></param>
        /// <returns></returns>
        [Trait("Priority", "1")]
        [Theory]
        [InlineData(nldasRequest, 365)]
        [InlineData(gldasRequest, 366)]
        [InlineData(daymetRequest, 365)]
        [InlineData(prismRequest, 365)]         //Passing?
        [InlineData(ncdcRequest, 365)]
        [InlineData(wgenRequest, 365)]
        public async Task ValidRequests(string precipInputString, int expected)
        {
            string endpoint = "api/hydrology/precipitation";
            PrecipitationInput input = JsonConvert.DeserializeObject<PrecipitationInput>(precipInputString);
            input.TemporalResolution = "daily";
            Debug.WriteLine("Integration Test: Precipitation controller; Endpoint: " + endpoint + "; Data source: " + input.Source);
            var response = await _client.PostAsync(
                endpoint, 
                new StringContent(JsonConvert.SerializeObject(input), Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            Assert.NotNull(result);
            TimeSeriesOutput resultObj = JsonConvert.DeserializeObject<TimeSeriesOutput>(result);
            Assert.Equal(expected, resultObj.Data.Count);
        }

    }
}
