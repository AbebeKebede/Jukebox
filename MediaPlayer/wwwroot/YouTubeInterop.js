// wwwroot/js/YouTubeInterop.js
var hasRendered = false;

initializeYouTubePlayer = (elementId, videoId, apiKey) => {
    console.log('Initializing YouTube Player');
    if (hasRendered) {
        // Initialize the YouTube IFrame Player if the component has rendered
        new YT.Player(elementId, {
            height: '360',
            width: '640',
            videoId: videoId,
            apiKey: apiKey,
            playerVars: {
                'playsinline': 1
            },
            events: {
                'onReady': onPlayerReady,
                'onStateChange': onPlayerStateChange
            }
        });
    }
};

function onPlayerReady(event) {
    // Do something when the player is ready
}

function onPlayerStateChange(event) {
    // Do something when the player state changes
}

document.addEventListener("DOMContentLoaded", function () {
    hasRendered = true;
});