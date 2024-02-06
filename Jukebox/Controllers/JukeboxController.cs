
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using System.Collections;
using Google.Apis.YouTube.v3.Data;

using Firebase.Database;
using Firebase.Database.Query;

namespace Jukebox.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JukeboxController : ControllerBase
    {
        private static List<Song> playlist = new List<Song> { };
        private const string firebaseUrl = "https://jukebox-407916-default-rtdb.firebaseio.com/";
        private static void RetrieveYouTubeMusicList()
        {
            // Replace "YOUR_API_KEY" with your actual YouTube Data API key
            string apiKey = "AIzaSyC2WwnNZfFI2Cke-LZs0a3iEQhT6XW4vC8";
            string channelId = "UCMVt-Q1oDAJJ5fgc0oS0tzw"; //UCMVt-Q1oDAJJ5fgc0oS0tzw UC_x5XG1OV2P6uZZ5FSM9TtwReplace with your desired channel ID

            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = apiKey,
                ApplicationName = "Jukebox"
            });

            var playlistItemsRequest = youtubeService.PlaylistItems.List("snippet");
            playlistItemsRequest.PlaylistId = "PLEneMfPMjHGdiFsa1CqzG_g7Q9vR4iQKG";//channelId;
            playlistItemsRequest.MaxResults = 50; // Adjust as needed
            

            var playlistItemsResponse = playlistItemsRequest.Execute();

            // Shuffle the playlist items
            var shuffledPlaylistItems = Shuffle<PlaylistItem>(playlistItemsResponse.Items);


            playlist.Clear();
            foreach (PlaylistItem playlistItem in shuffledPlaylistItems)
            {
                string title = playlistItem.Snippet.Title;
                string videoId = playlistItem.Snippet.ResourceId.VideoId;
                string videoURL = $"https://www.youtube.com/watch?v={videoId}";
                string thumbnails = playlistItem.Snippet.Thumbnails.Medium.Url;
                // Create a new Song object and add it to the playlist
                _ = AddSong(new Song { Id = playlist.Count + 1, Title = title, Votes = 0, VideoURL = videoURL, Thumbnails = thumbnails });
            }

            
        }

        private static async Task AddSong(Song newSong)
        {
            // Replace with your Firebase database URL
            

            // Initialize FirebaseClient
            var firebaseClient = new FirebaseClient(firebaseUrl);
                        

            // Reference to the Songs node in the database
            var songsReference = firebaseClient.Child("Songs");

            try
            {
                // Push the new song to the database
                var addedSong = await songsReference.PostAsync(newSong);
               
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding song: {ex.Message}");
            }
        }


        // Shuffle the list using the Fisher-Yates algorithm
        static IList<T> Shuffle<T>(IList<T> list)
        {
            Random rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            return list;
        }

        [HttpGet]
        public ActionResult<IEnumerable<Song>> GetPlaylist()
        {
            //RetrieveYouTubeMusicList();
            Task task = GetSongs();
            return Ok(playlist.OrderByDescending(s => s.Votes));
        }

        private static async Task GetSongs()
        {
           

            // Initialize FirebaseClient
            var firebaseClient = new FirebaseClient(firebaseUrl);

            // Reference to the Songs node in the database
            var songsReference = firebaseClient.Child("Songs");

            try
            {
                // Retrieve the list of songs from the database
                var songs = await songsReference.OnceAsync<Song>();

                // Print the retrieved songs
                foreach (var song in songs)
                {
                    //Console.WriteLine($"Song Id: {song.Object.Id}, Title: {song.Object.Title}");
                    playlist.Add(new Song { Id = playlist.Count + 1, 
                                            Title = song.Object.Title, 
                                            Votes = 0, 
                                            VideoURL = song.Object.VideoURL, 
                                            Thumbnails = song.Object.Thumbnails, 
                                            Key= song.Key});
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving songs: {ex.Message}");
            }
        }

        [HttpPost("{songKey}/vote")]
        public async void Vote(string songKey)
        {

            var vote = new Vote { Id = 1,DateTimeVoted=DateTime.Now,IsCurrentVote=true,SongKey= songKey }; //playlist.FirstOrDefault(v => v.Id == id);

            // Simulate a vote; in a real-world scenario, you would likely authenticate users
            vote.Votes++;
            await NewVote(vote);
            //return Ok(vote);
        }

        private static async Task NewVote(Vote newVote)
        {
            

            // Initialize FirebaseClient
            var firebaseClient = new FirebaseClient(firebaseUrl);

           
            // Reference to the Votes node in the database
            var votesReference = firebaseClient.Child("Votes");

            try
            {
                // Push the new vote to the database
                var addedVote = await votesReference.PostAsync(newVote);

                Console.WriteLine($"Vote added with key: {addedVote.Key}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding vote: {ex.Message}");
            }
        }
        [HttpGet("/playsong")]
        public async Task<string> PlaySong()
        {
            try
            {
                
                // Initialize FirebaseClient
                var firebaseClient = new FirebaseClient(firebaseUrl);

                // Reference to the Votes node in the database
                var votesReference = firebaseClient.Child("Votes");

                // Retrieve the list of votes from the database
                var votes = await votesReference.OnceAsync<Vote>();

                if (votes != null && votes.Any())
                {
                    var highestVotesPerSong = votes
                        .GroupBy(v => v.Object.SongKey)
                        .Select(group => group.OrderByDescending(v => v.Object.Votes).FirstOrDefault())
                        .Where(highestVote => highestVote != null)
                        .ToList();

                    if (highestVotesPerSong.Any())
                    {
                        var highestVote = highestVotesPerSong.FirstOrDefault();
                        if (highestVote != null)
                        {
                            var songsReference = firebaseClient.Child("Songs");

                            if (highestVote.Object.SongKey != null)
                            {
                                var highestVoteSong = await songsReference.Child(highestVote.Object.SongKey.ToString()).OnceSingleAsync<Song>();

                                if (highestVoteSong != null)
                                {
                                    return highestVoteSong.VideoURL;
                                }
                            }
                        }
                    }
                }

                // Handle the case where there are no votes or some other issue
                return "No valid song URL found.";
            }
            catch (Exception ex)
            {
                // Log or handle the exception according to your application's requirements
                return $"An error occurred: {ex.Message}";
            }
        }
        // Add other methods for integrating with music providers, managing the playlist, etc.

    }

    public class Song
    {
        public string Key { get; set; }
        public int Id { get; set; }
        public string Title { get; set; }
        public int Votes { get; set; }
        public string VideoURL { get; set; }
        public string Thumbnails { get; set; }
    }

    public class Vote
    {
        public int Id { get; set; }
        public string SongKey { get; set; }
        public int SongId { get; set; }
        public int Votes { get; set; }
        public Boolean IsCurrentVote { get; set; }
        public DateTime DateTimeVoted { get; set; }
    }

    public class PlayList
    {
        public int Id { get; set; }
        public int SongId { get; set; }
        public int NumberOfTimesPlayed { get; set;}
    }
}


