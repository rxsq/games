using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SkiaSharp;

public class MainForm : Form
{
    private Label lblTitle;
    private Button btnTargetGame;
    private Button btnSmashGame;
    private Button btnChaserGame;
    private TextBox txtGameDescription;
    private Panel panelBackground;
    private PictureBox movingBackground;
    private Label lblScore;
    private Label lblLevel;
    private ProgressBar progressBar;
    private int score;
    private int level;

    public MainForm()
    {
        InitializeComponent();
        UpdateUI();
    }

    private void InitializeComponent()
    {
        this.Text = "Game Selection";
        this.Size = new Size(800, 600);
        this.BackColor = Color.Black;

        panelBackground = new Panel
        {
            Dock = DockStyle.Fill,
            BackgroundImage = LoadWebPImage("content/background.webp"),
            BackgroundImageLayout = ImageLayout.Stretch
        };

        movingBackground = new PictureBox
        {
            Image = LoadWebPImage("content/moving_effect.webp"),
            SizeMode = PictureBoxSizeMode.StretchImage,
            Dock = DockStyle.Fill
        };

        lblTitle = new Label
        {
            Text = "Select Your Game",
            Font = new Font("Arial", 24, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 50
        };

        btnTargetGame = new Button
        {
            Text = "Target Game",
            Font = new Font("Arial", 16, FontStyle.Bold),
            Size = new Size(200, 50),
            Location = new Point(300, 100),
            BackColor = Color.Black,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnTargetGame.FlatAppearance.BorderColor = Color.LimeGreen;
        btnTargetGame.FlatAppearance.BorderSize = 2;
        btnTargetGame.Click += (sender, e) => StartGame("Target");

        btnSmashGame = new Button
        {
            Text = "Smash Game",
            Font = new Font("Arial", 16, FontStyle.Bold),
            Size = new Size(200, 50),
            Location = new Point(300, 200),
            BackColor = Color.Black,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnSmashGame.FlatAppearance.BorderColor = Color.DeepPink;
        btnSmashGame.FlatAppearance.BorderSize = 2;
        btnSmashGame.Click += (sender, e) => StartGame("Smash");

        btnChaserGame = new Button
        {
            Text = "Chaser Game",
            Font = new Font("Arial", 16, FontStyle.Bold),
            Size = new Size(200, 50),
            Location = new Point(300, 300),
            BackColor = Color.Black,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnChaserGame.FlatAppearance.BorderColor = Color.Cyan;
        btnChaserGame.FlatAppearance.BorderSize = 2;
        btnChaserGame.Click += (sender, e) => StartGame("Chaser");

        txtGameDescription = new TextBox
        {
            Font = new Font("Arial", 14),
            ForeColor = Color.White,
            BackColor = Color.Black,
            Multiline = true,
            ReadOnly = true,
            Size = new Size(600, 150),
            Location = new Point(100, 400),
            BorderStyle = BorderStyle.None
        };

        lblScore = new Label
        {
            Text = "Score: 0",
            Font = new Font("Arial", 14, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(10, 60),
            Width = 100
        };

        lblLevel = new Label
        {
            Text = "Level: 1",
            Font = new Font("Arial", 14, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(10, 90),
            Width = 100
        };

        progressBar = new ProgressBar
        {
            Location = new Point(10, 120),
            Width = 200,
            Height = 20,
            Maximum = 100
        };

        this.Controls.Add(panelBackground);
        panelBackground.Controls.Add(movingBackground);
        panelBackground.Controls.Add(lblTitle);
        panelBackground.Controls.Add(btnTargetGame);
        panelBackground.Controls.Add(btnSmashGame);
        panelBackground.Controls.Add(btnChaserGame);
        panelBackground.Controls.Add(txtGameDescription);
        panelBackground.Controls.Add(lblScore);
        panelBackground.Controls.Add(lblLevel);
        panelBackground.Controls.Add(progressBar);

        // Ensure buttons and labels are on top
        lblTitle.BringToFront();
        btnTargetGame.BringToFront();
        btnSmashGame.BringToFront();
        btnChaserGame.BringToFront();
        txtGameDescription.BringToFront();
        lblScore.BringToFront();
        lblLevel.BringToFront();
        progressBar.BringToFront();
    }
    BaseGame currentGame;
    private void StartGame(string gameType)
    {
        ShowGameDescription(gameType, GetGameDescription(gameType));
        // Start the selected game
        switch (gameType)
        {
            case "Target":
               
                   
                    currentGame = new Target(TimeSpan.FromMinutes(1), "c:\\games\\logs\\target_game_log.log", new UdpHandler("192.168.0.7", 21, 7113, "c:\\games\\logs\\udp_log.log"), new MusicPlayer());
                    currentGame.StartGame();
               

                break;
            case "Smash":
               
                currentGame = new Smash(TimeSpan.FromMinutes(1), "c:\\games\\logs\\target_game_log.log", new UdpHandler("192.168.0.7", 21, 7113, "c:\\games\\logs\\udp_log.log"), new MusicPlayer());
                currentGame.StartGame();
                break;
            case "Chaser":
              //  currentGame?.EndGame();
              //  currentGame = new LightChaserGame(TimeSpan.FromMinutes(1), "c:\\games\\logs\\target_game_log.log", new UdpHandler("192.168.0.7", 21, 7113, "c:\\games\\logs\\udp_log.log"), new MusicPlayer());
             //   currentGame.StartGame();
                break;
        }

        // Example of updating UI based on game start
        score = 0;
        level = 1;
        UpdateUI();
    }

    private string GetGameDescription(string gameType)
    {
        switch (gameType)
        {
            case "Target":
                return "In the Target Game, hit the highlighted targets as quickly as possible. Each successful hit scores points.";
            case "Smash":
                return "In the Smash Game, smash the targets that light up. The faster you smash, the higher your score.";
            case "Chaser":
                return "In the Chaser Game, chase and hit the moving targets. Stay quick and keep up to score points.";
            default:
                return "";
        }
    }

    private void ShowGameDescription(string gameTitle, string description)
    {
        txtGameDescription.Text = $"{gameTitle}\r\n\r\n{description}";
    }

    private void UpdateUI()
    {
        lblScore.Text = $"Score: {score}";
        lblLevel.Text = $"Level: {level}";
        progressBar.Value = 0;
    }

    private Image LoadWebPImage(string filePath)
    {
        using (var inputStream = new SKManagedStream(File.OpenRead(filePath)))
        {
            using (var codec = SKCodec.Create(inputStream))
            {
                var info = codec.Info;
                var bitmap = new SKBitmap(info.Width, info.Height);
                var result = codec.GetPixels(bitmap.Info, bitmap.GetPixels());

                if (result == SKCodecResult.Success || result == SKCodecResult.IncompleteInput)
                {
                    return BitmapFromSKBitmap(bitmap);
                }
                else
                {
                    throw new Exception("Failed to load WebP image.");
                }
            }
        }
    }

    private Bitmap BitmapFromSKBitmap(SKBitmap skBitmap)
    {
        var image = SKImage.FromBitmap(skBitmap);
        var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using (var stream = new MemoryStream())
        {
            data.SaveTo(stream);
            stream.Position = 0;
            return new Bitmap(stream);
        }
    }

   
}
