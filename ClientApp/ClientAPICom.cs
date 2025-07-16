using System;
using System.Diagnostics.Eventing.Reader;
using System.Text.Json;
using System.Threading.Tasks;
using ClassLibrary.Coordinates;
using ClassLibrary.Responses;
using ClassLibrary.GlobalVariables;

namespace ClientApp.Communication;
public class ClientAPICom
{
    private readonly HttpClient httpClient;
    private readonly JsonSerializerOptions jsonOptions;

    /// <summary>
    /// Initializes a new instance of the ClientAPICom class.
    /// </summary>
    public ClientAPICom()
    {
        httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:5049");
        jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    /// <summary>
    /// Logs in the user and returns the GameID.
    /// </summary>
    public async Task<string> LoginAsync()
    {
        try
        {
            HttpResponseMessage response = await httpClient.GetAsync("/API/login");
            string content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return content; // Returns the GameID
            }
            else if ((int)response.StatusCode == 570)
            {
                return "Login failed because user already exists.";
            }
            else if ((int)response.StatusCode == 571)
            {
                return "Login failed because game is already in progress.";
            }
            else
            {
                return "Login failed because of unknown reason.";
            }
        }
        catch (Exception ex)
        {
            return $"Error during login: {ex.Message}";
        }
    }

    /// <summary>
    /// Looks at the map.
    /// </summary>
    public async Task<Response?> LookAsync()
    {
        try
        {
            HttpResponseMessage response = await httpClient.GetAsync("/API/look");
            string content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<Response>(content, jsonOptions);
            }
            else if ((int)response.StatusCode == 550)
            {
                return new Response { Message = "Look failed because your game was not found." };
            }
            else if ((int)response.StatusCode == 551)
            {
                return new Response { Message = "Look failed because your game is already finished." };
            }
            else
            {
                return new Response { Message = "Look failed for unknown reason." };
            }
        }
        catch (Exception ex)
        {
            return new Response { Message = $"Error during look: {ex.Message}" };
        }
    }

    /// <summary>
    /// Moves the player to the specified coordinates.
    /// </summary>
    public async Task<Response?> MoveAsync(int x, int y)
    {
        try
        {
            HttpResponseMessage response = await httpClient.GetAsync($"/API/move/{x}/{y}");
            string content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<Response>(content, jsonOptions);
            }
            else if ((int)response.StatusCode == 550)
            {
                return new Response { Message = "Move failed because your game was not found." };
            }
            else if ((int)response.StatusCode == 551)
            {
                return new Response { Message = "Move failed because your game is already finished." };
            }
            else if ((int)response.StatusCode == 564)
            {
                return new Response { Message = "Move failed because there is an obstacle in the way." };
            }
            else if ((int)response.StatusCode == 563)
            {
                return new Response { Message = "Move failed because you can only move to adjacent tiles." };
            }
            else if ((int)response.StatusCode == 562)
            {
                return new Response { Message = "Move failed because you have reached the map border." };
            }
            else
            {
                return new Response { Message = "Move failed for unknown reason." };
            }
        }
        catch (Exception ex)
        {
            return new Response { Message = $"Error during move: {ex.Message}" };
        }
    }

    /// <summary>
    /// Tries to collect the item at the player's current position.
    /// </summary>
    public async Task<Response?> CollectAsync()
    {
        try
        {
            HttpResponseMessage response = await httpClient.GetAsync("/API/collect");
            string content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<Response>(content, jsonOptions);
            }
            else if ((int)response.StatusCode == 550)
            {
                return new Response { Message = "Collect failed because your game was not found." };
            }
            else if ((int)response.StatusCode == 551)
            {
                return new Response { Message = "Collect failed because your game is already finished." };
            }
            else if ((int)response.StatusCode == 566)
            {
                return new Response { Message = "Collect failed because there is no item to collect." };
            }
            else
            {
                return new Response { Message = "Collect failed for unknown reason." };
            }
        }
        catch (Exception ex)
        {
            return new Response { Message = $"Error during collect: {ex.Message}" };
        }
    }

    /// <summary>
    /// Quits the game.
    /// </summary>
    public async Task<Response?> QuitAsync()
    {
        try
        {
            HttpResponseMessage response = await httpClient.GetAsync("/API/quit");
            string content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<Response>(content, jsonOptions);
            }
            else if ((int)response.StatusCode == 550)
            {
                return new Response { Message = "Quit failed because your game was not found." };
            }
            else if ((int)response.StatusCode == 551)
            {
                return new Response { Message = "Quit failed because your game is already finished." };
            }
            else
            {
                return new Response { Message = "Quit failed for unknown reason." };
            }
        }
        catch (Exception ex)
        {
            return new Response { Message = $"Error during quit: {ex.Message}" };
        }
    }

    /// <summary>
    /// Tries to finish the game.
    /// </summary>
    public async Task<Response?> FinishAsync()
    {
        try
        {
            HttpResponseMessage response = await httpClient.GetAsync("/API/finish");
            string content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                GV.finished = true;
                return JsonSerializer.Deserialize<Response>(content, jsonOptions);
            }
            else if ((int)response.StatusCode == 550)
            {
                return new Response { Message = "Finish failed because your game was not found." };
            }
            else if ((int)response.StatusCode == 551)
            {
                return new Response { Message = "Finish failed because your game is already finished." };
            }
            else if ((int)response.StatusCode == 400)
            {
                return new Response { Message = "Finish failed because there are still coins left." };
            }
            else
            {
                return new Response { Message = JsonSerializer.Deserialize<Response>(content, jsonOptions).Message };
            }
        }
        catch (Exception ex)
        {
            return new Response { Message = $"Error during finish: {ex.Message}" };
        }
    }
}