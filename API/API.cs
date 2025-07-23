using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ClassLibrary.GlobalVariables;
using ClassLibrary.Objects;
using ClassLibrary.Coordinates;

namespace API
{
    /// <summary>
    /// Response model for game login API calls
    /// Contains the GameID returned by the server
    /// </summary>
    public class GameResponse
    {
        /// <summary>
        /// Unique identifier for the game session returned by the server
        /// </summary>
        public required string GameID { get; set; }
    }

    /// <summary>
    /// Response model for finish game API calls
    /// Contains whether the game was successfully finished
    /// </summary>
    public class FinishResponse
    {
        /// <summary>
        /// Whether the game was successfully finished ("yes" or "no")
        /// </summary>
        public required string Finished { get; set; }

        /// <summary>
        /// Message explaining the finish result
        /// </summary>
        public required string Message { get; set; }
    }

    ////////////////////LOGIN////////////////////
    #region Login

    /// <summary>
    /// Main API controller for game operations.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class APIController : ControllerBase
    {
        /// <summary>
        /// Initiates a new game session by logging into thegame server.
        /// Resets the coordinate system and clears any existing map data for a fresh start.
        /// </summary>
        /// <returns>ActionResult containing the GameID if successful, or error information if failed</returns>
        [HttpGet("login")]
        public async Task<IActionResult> Login(string Map)
        {
            // Reset coordinate system and clear map for new game
            CoordinateConverter.SetPlayerPosition(0, 0);
            CoordinateConverter.ClearGlobalMap();
            using HttpClient client = new HttpClient();

            // Construct HTTP request to start a new game
            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://{GV.Host}:{GV.Port}/PASProject/web/NewGame/Map/{Map ?? GV.CurrentMap}/User/{GV.User}"),
                Content = new StringContent("{\"hashedPassword\":\"" + GV.HashedPassword + "\"}")
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("application/json")
                    }
                }
            };

            // Send request to game server
            using HttpResponseMessage response = await client.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            /*
            // Log response details for debugging
            Console.WriteLine($"Status Code: {(int)response.StatusCode}");
            Console.WriteLine("Response Body:");
            Console.WriteLine(responseBody);
            */

            // Handle unsuccessful response from game server
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, new
                {
                    Error = "Game start failed",
                    StatusCode = (int)response.StatusCode,
                    Response = responseBody
                });
            }

            try
            {
                // Parse the GameID from the server response
                GameResponse? game = JsonSerializer.Deserialize<GameResponse>(responseBody);
                if (game != null)
                {
                    // Store the GameID globally for use in other requests
                    return Ok(GV.GameId = game.GameID);
                }
                else
                {
                    return StatusCode(500, new
                    {
                        Error = "Failed to parse GameID - response is null",
                        RawResponse = responseBody
                    });
                }
            }
            catch (JsonException ex)
            {
                // Handle JSON parsing errors
                return StatusCode(500, new
                {
                    Error = "Failed to parse GameID",
                    Exception = ex.Message,
                    RawResponse = responseBody
                });
            }
        }

        #endregion
        ////////////////////QUIT////////////////////
        #region Quit

        /// <summary>
        /// Terminates the current game session.
        /// </summary>
        /// <returns>ActionResult indicating success or failure of the quit operation</returns>
        [HttpGet("quit")]
        public async Task<IActionResult> QuitGame()
        {
            using HttpClient client = new HttpClient();

            // Construct quit request using the current game session ID
            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://{GV.Host}:{GV.Port}/PASProject/web/Game/{GV.GameId}/Quit")
            };

            // Send quit request to game server
            using HttpResponseMessage response = await client.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            /*
            // Log response details for debugging
            Console.WriteLine($"Status Code: {(int)response.StatusCode}");
            Console.WriteLine("Response Body:");
            Console.WriteLine(responseBody);
            */

            // Handle unsuccessful quit operation
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, new
                {
                    Error = "Game quit failed",
                    StatusCode = (int)response.StatusCode,
                    Response = responseBody
                });
            }

            // Return success response
            return Ok(new
            {
                Message = "Game quit was successful.",
                Response = responseBody
            });
        }

        #endregion
        //////////////////////FINISH////////////////////
        #region Finish

        /// <summary>
        /// Marks the current game as finished
        /// </summary>
        /// <returns>ActionResult indicating success or failure of the finish operation</returns>
        [HttpGet("finish")]
        public async Task<IActionResult> FinishGame()
        {
            using HttpClient client = new HttpClient();

            // Construct finish request using the current game session ID
            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://{GV.Host}:{GV.Port}/PASProject/web/Game/{GV.GameId}/Finished")
            };

            // Send finish request to game server
            using HttpResponseMessage response = await client.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            /*
            // Log response details for debugging
            Console.WriteLine($"Status Code: {(int)response.StatusCode}");
            Console.WriteLine("Response Body:");
            Console.WriteLine(responseBody);
            */

            // Handle unsuccessful finish operation
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, new
                {
                    Error = "Game finish failed",
                    StatusCode = (int)response.StatusCode,
                    Response = responseBody
                });
            }

            try
            {
                // Configure JSON options for case-insensitive property matching
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Parse the JSON response to check if game was actually finished
                FinishResponse? finishResponse = JsonSerializer.Deserialize<FinishResponse>(responseBody, options);

                if (finishResponse != null)
                {
                    // Check if the game was successfully finished
                    if (finishResponse.Finished.Equals("yes", StringComparison.OrdinalIgnoreCase))
                    {
                        return StatusCode(200, new
                        {
                            Message = $"Game finish was successful: {finishResponse.Message}",
                            Response = responseBody
                        });
                    }
                    else
                    {
                        return BadRequest(new
                        {
                            Error = $"Game finish failed: {finishResponse.Message}",
                            Response = responseBody
                        });
                    }
                }
                else
                {
                    return StatusCode(500, new
                    {
                        Error = "Failed to parse finish response - response is null",
                        RawResponse = responseBody
                    });
                }
            }
            catch (JsonException ex)
            {
                // If JSON parsing fails, try to extract info manually from the raw response
                if (responseBody.Contains("\"Finished\":\"no\"", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new
                    {
                        Error = "Game finish failed - there are still pickable elements remaining",
                        Response = responseBody
                    });
                }
                else if (responseBody.Contains("\"Finished\":\"yes\"", StringComparison.OrdinalIgnoreCase))
                {
                    return Ok(new
                    {
                        Message = "Game finish was successful.",
                        Response = responseBody
                    });
                }
                else
                {
                    return StatusCode(500, new
                    {
                        Error = "Failed to parse finish response",
                        Exception = ex.Message,
                        RawResponse = responseBody
                    });
                }
            }
        }

        #endregion
        //////////////////////LOOK////////////////////
        #region Look

        /// <summary>
        /// Retrieves information about objects in the surrounding area.
        /// Update the global map with absolute coordinates.
        /// </summary>
        /// <returns>ActionResult containing objects with both relative and absolute coordinates, current position, and map stats</returns>
        [HttpGet("look")]
        public async Task<IActionResult> Look()
        {
            using HttpClient client = new HttpClient();

            // Construct look request using the current game session ID
            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://{GV.Host}:{GV.Port}/PASProject/web/Game/{GV.GameId}/Look")
            };

            // Send look request to game server
            using HttpResponseMessage response = await client.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            /*
            // Log response details for debugging
            Console.WriteLine($"Status Code: {(int)response.StatusCode}");
            Console.WriteLine("Response Body:");
            Console.WriteLine(responseBody);
            */

            // Handle unsuccessful look operation
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, new
                {
                    Error = "Look failed",
                    StatusCode = (int)response.StatusCode,
                    Response = responseBody
                });
            }

            try
            {
                // Parse the JSON response into structured data
                LookResponse? lookResponse = JsonSerializer.Deserialize<LookResponse>(responseBody);

                if (lookResponse?.Look != null)
                {
                    // Log discovered objects for debugging
                    Console.WriteLine($"Found {lookResponse.Look.Count} objects:");
                    foreach (var item in lookResponse.Look)
                    {
                        var o = item.Object;
                        Console.WriteLine($"Object at ({o.CoordX}, {o.CoordY}): {o.ObjectName} ({o.ObjectType})");
                    }

                    // Extract object data from the response structure
                    var objects = lookResponse.Look.Select(item => item.Object).ToList();

                    // Try to establish absolute coordinate system using start position marker
                    CoordinateConverter.FindAndSetStartPosition(objects);

                    // Convert relative coordinates to absolute coordinates
                    var absoluteObjects = CoordinateConverter.ConvertToAbsolute(objects);

                    // Add discovered objects to the global map 
                    CoordinateConverter.AddToGlobalMap(absoluteObjects);

                    // Return structured response with coordinate information and map stats
                    return Ok(new
                    {
                        Message = "Looking was successful.",
                        CurrentPosition = new
                        {
                            AbsoluteX = CoordinateConverter.CurrentAbsoluteX,
                            AbsoluteY = CoordinateConverter.CurrentAbsoluteY
                        },
                        Objects = absoluteObjects.Select(abs => new
                        {
                            RelativeX = abs.RelativeX,
                            RelativeY = abs.RelativeY,
                            AbsoluteX = abs.AbsoluteX,
                            AbsoluteY = abs.AbsoluteY,
                            Name = abs.Name,
                            Type = abs.Type
                        }),
                        MapStats = CoordinateConverter.GetMapStatistics()
                    });
                }
                else
                {
                    return StatusCode(500, new
                    {
                        Error = "Look response is null or invalid",
                        RawResponse = responseBody
                    });
                }
            }
            catch (JsonException ex)
            {
                // Handle JSON parsing errors
                return StatusCode(500, new
                {
                    Error = "Failed to parse look response",
                    Exception = ex.Message,
                    RawResponse = responseBody
                });
            }
        }

        #endregion
        /////////////////////////COLLECT////////////////////
        #region Collect

        /// <summary>
        /// Attempts to collect an item at the player's current position.
        /// </summary>
        /// <returns>ActionResult indicating success or failure of the collection operation</returns>
        [HttpGet("collect")]
        public async Task<IActionResult> Collect()
        {
            using HttpClient client = new HttpClient();

            // Construct collect request using the current game session ID
            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://{GV.Host}:{GV.Port}/PASProject/web/Game/{GV.GameId}/Collect")
            };

            // Send collect request to game server
            using HttpResponseMessage response = await client.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            /*
            // Log response details for debugging
            Console.WriteLine($"Status Code: {(int)response.StatusCode}");
            Console.WriteLine("Response Body:");
            Console.WriteLine(responseBody);
            */

            // Handle unsuccessful collection operation
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, new
                {
                    Error = "Collection failed",
                    StatusCode = (int)response.StatusCode,
                    Response = responseBody
                });
            }

            // Return success response with raw server response
            return Ok(new
            {
                Message = "Collecting was successful.",
                Response = responseBody
            });
        }

        #endregion
        ///////////////////////MOVE////////////////////
        #region Move

        /// <summary>
        /// Attempts to move the player by the specified delta coordinates.
        /// Updates the player's position in the coordinate system and processes the response to update the global map with newly visible objects.
        /// </summary>
        /// <param name="x">Delta X movement (positive = right, negative = left)</param>
        /// <param name="y">Delta Y movement (positive = up, negative = down)</param>
        /// <returns>ActionResult containing movement result, new position, visible objects with coordinates, and map stats</returns>
        [HttpGet("move/{x}/{y}")]
        public async Task<IActionResult> Move(int x, int y)
        {
            using HttpClient client = new HttpClient();

            // Construct move request with delta coordinates using the current game session ID
            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://{GV.Host}:{GV.Port}/PASProject/web/Game/{GV.GameId}/Move/{x}/{y}")
            };

            // Send move request to game server
            using HttpResponseMessage response = await client.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            /*
            // Log response details for debugging
            Console.WriteLine($"Status Code: {(int)response.StatusCode}");
            Console.WriteLine("Response Body:");
            Console.WriteLine(responseBody);
            */

            // Handle unsuccessful move operation
            if (!response.IsSuccessStatusCode)
            {
                CoordinateConverter.UpdatePlayerPosition(0, 0);
                return StatusCode((int)response.StatusCode, new
                {
                    Error = "Moving failed",
                    StatusCode = (int)response.StatusCode,
                    Response = responseBody
                });
            }

            try
            {
                // Parse the JSON response into structured data
                MoveResponse? moveResponse = JsonSerializer.Deserialize<MoveResponse>(responseBody);

                if (moveResponse?.Move != null)
                {
                    // Log discovered objects for debugging
                    Console.WriteLine($"Found {moveResponse.Move.Count} objects:");
                    foreach (var item in moveResponse.Move)
                    {
                        var o = item.Object;
                        Console.WriteLine($"Object at ({o.CoordX}, {o.CoordY}): {o.ObjectName} ({o.ObjectType})");
                    }

                    // Update player's absolute position with the movement delta
                    CoordinateConverter.UpdatePlayerPosition(x, y);

                    // Extract object data from the nested response structure
                    var objects = moveResponse.Move.Select(item => item.Object).ToList();

                    // Convert relative coordinates to absolute coordinates
                    var absoluteObjects = CoordinateConverter.ConvertToAbsolute(objects);

                    // Add discovered objects to the global map for persistence
                    CoordinateConverter.AddToGlobalMap(absoluteObjects);

                    // Return structured response with new position, coordinate information, and map stats
                    return Ok(new
                    {
                        Message = "Moving was successful.",
                        CurrentPosition = new
                        {
                            AbsoluteX = CoordinateConverter.CurrentAbsoluteX,
                            AbsoluteY = CoordinateConverter.CurrentAbsoluteY
                        },
                        Objects = absoluteObjects.Select(abs => new
                        {
                            RelativeX = abs.RelativeX,
                            RelativeY = abs.RelativeY,
                            AbsoluteX = abs.AbsoluteX,
                            AbsoluteY = abs.AbsoluteY,
                            Name = abs.Name,
                            Type = abs.Type
                        }),
                        MapStats = CoordinateConverter.GetMapStatistics()
                    });
                }
                else
                {
                    return StatusCode(500, new
                    {
                        Error = "Move response is null or invalid",
                        RawResponse = responseBody
                    });
                }
            }
            catch (JsonException ex)
            {
                // Handle JSON parsing errors
                return StatusCode(500, new
                {
                    Error = "Failed to parse move response",
                    Exception = ex.Message,
                    RawResponse = responseBody
                });
            }
        }
    }
}
#endregion