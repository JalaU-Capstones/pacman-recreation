namespace PacmanGame.Helpers;

/// <summary>
/// Game constants and configuration values
/// </summary>
public static class Constants
{
    // Grid/Map Constants
    public const int MapWidth = 28;
    public const int MapHeight = 31;
    public const int TileSize = 32; // pixels per tile

    // Game Window
    public const int WindowWidth = MapWidth * TileSize;   // 896 pixels
    public const int WindowHeight = MapHeight * TileSize;  // 992 pixels
    public const int HudHeight = 80;  // Additional height for HUD
    public const int TotalWindowHeight = WindowHeight + HudHeight;

    // Game Speed
    public const float TargetFps = 60.0f;
    public const float FixedDeltaTime = 1.0f / TargetFps;

    // Entity Speeds (tiles per second)
    public const float PacmanSpeed = 4.0f;
    public const float GhostNormalSpeed = 3.7f;
    public const float GhostVulnerableSpeed = 2.0f;
    public const float GhostEatenSpeed = 6.0f;

    // Power Pellet
    public const float PowerPelletDuration = 6.0f;  // seconds
    public const float PowerPelletWarningTime = 2.0f;  // seconds before ending

    // Scoring
    public const int SmallDotPoints = 10;
    public const int PowerPelletPoints = 50;
    public const int GhostPoints = 200;  // Base points, multiplies with combo
    public const int CherryPoints = 100;
    public const int StrawberryPoints = 300;
    public const int OrangePoints = 500;
    public const int ApplePoints = 700;
    public const int MelonPoints = 1000;
    public const int ExtraLifeScore = 10000;  // Score needed for extra life

    // Lives
    public const int StartingLives = 3;
    public const int MaxLives = 5;

    // Animation
    public const float AnimationSpeed = 0.1f;  // seconds per frame
    public const int PacmanAnimationFrames = 3;
    public const int GhostAnimationFrames = 2;
    public const int DeathAnimationFrames = 11;

    // Ghost AI
    public const float ModeToggleInterval = 20.0f; // seconds
    public const int ClydeShyDistance = 8; // tiles
    public const int BlinkyScatterY = 0;
    public const int BlinkyScatterX = 27;
    public const int PinkyScatterY = 0;
    public const int PinkyScatterX = 0;
    public const int InkyScatterY = 30;
    public const int InkyScatterX = 27;
    public const int ClydeScatterY = 30;
    public const int ClydeScatterX = 0;

    // File Paths
    public const string AssetsPath = "Assets";
    public const string SpritesPath = "Assets/Sprites";
    public const string AudioPath = "Assets/Audio";
    public const string MusicPath = "Assets/Audio/Music";
    public const string SfxPath = "Assets/Audio/SFX";
    public const string MapsPath = "Assets/Maps";

    // Sprite Sheets
    public const string PacmanSpriteSheet = "pacman_spritesheet.png";
    public const string GhostsSpriteSheet = "ghosts_spritesheet.png";
    public const string ItemsSpriteSheet = "items_spritesheet.png";
    public const string TilesSpriteSheet = "tiles_spritesheet.png";

    // Sprite Maps (JSON)
    public const string PacmanSpriteMap = "pacman_sprite_map.json";
    public const string GhostsSpriteMap = "ghosts_sprite_map.json";
    public const string ItemsSpriteMap = "items_sprite_map.json";
    public const string TilesSpriteMap = "tiles_sprite_map.json";

    // Music Files
    public const string BackgroundMusic = "background-theme.wav";
    public const string MenuMusic = "menu-theme.wav";
    public const string GameOverMusic = "game-over-theme.wav";

    // Sound Effects
    public const string ChompSound = "chomp.wav";
    public const string EatPowerPelletSound = "eat-power-pellet.wav";
    public const string EatGhostSound = "eat-ghost.wav";
    public const string EatFruitSound = "eat-fruit.wav";
    public const string DeathSound = "death.wav";
    public const string ExtraLifeSound = "extra-life.wav";
    public const string GameStartSound = "game-start.wav";
    public const string LevelCompleteSound = "level-complete.wav";
    public const string GameOverSound = "game-over.wav";
    public const string MenuSelectSound = "menu-select.wav";
    public const string MenuNavigateSound = "menu-navigate.wav";
    public const string GhostReturnSound = "ghost-return.wav";

    // Map Files
    public const string Level1Map = "level1.txt";
    public const string Level2Map = "level2.txt";
    public const string Level3Map = "level3.txt";

    // Map Characters
    public const char WallChar = '#';
    public const char SmallDotChar = '.';
    public const char PowerPelletChar = 'o';
    public const char PacmanChar = 'P';
    public const char GhostChar = 'G';
    public const char GhostDoorChar = '-';
    public const char FruitChar = 'F';
    public const char EmptyChar = ' ';

    // Colors (RGB Hex)
    public const string PacmanYellow = "#FFFF00";
    public const string MazeBlue = "#2121FF";
    public const string BackgroundBlack = "#000000";
    public const string BlinkyRed = "#FF0000";
    public const string PinkyPink = "#FFB8FF";
    public const string InkyCyan = "#00FFFF";
    public const string ClydeOrange = "#FFB851";
    public const string VulnerableBlue = "#2121DE";
    public const string VulnerableWarning = "#FFFFFF";

    // Ghost Names
    public const string BlinkyName = "Blinky";
    public const string PinkyName = "Pinky";
    public const string InkyName = "Inky";
    public const string ClydeName = "Clyde";

    // Save File
    public const string ScoresFileName = "scores.txt";
    public const int MaxHighScores = 10;
}
