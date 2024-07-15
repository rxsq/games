using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection.Emit;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

public partial class MainForm : Form
{
    private List<Point> stars;
    private Timer starTimer;
    private Random random = new Random();

    public MainForm()
    {
        InitializeComponent();
        LoadPictures();
        InitializeStars();
      
        StartStarAnimation();
        
    }
    BaseGame currentGame = null;
    private void StartGame(string gameType)
    {
        ShowGameDescription(gameType, GetGameDescription(gameType));
        // Start the selected game
        if(currentGame != null )
        {
            currentGame.EndGame();
        }
        switch (gameType)
        {
            case "Target":
                currentGame = new Target(new GameConfig { Maxiterations = 2, MaxLevel = 5, MaxPlayers = 5, MaxIterationTime = 30, ReductionTimeEachLevel = 5, NoofLedPerdevice =3},18);
                break;
            case "Smash":
                currentGame = new Smash(new GameConfig { Maxiterations = 3, MaxLevel = 5, MaxPlayers = 2, MaxIterationTime = 60, ReductionTimeEachLevel = 10, NoofLedPerdevice = 3 },.2);
                break;
            case "Chaser":
                currentGame = new Chaser(new GameConfig { Maxiterations = 3, MaxLevel = 5, MaxPlayers = 2, MaxIterationTime = 60, ReductionTimeEachLevel = 10, NoofLedPerdevice = 3 });
                break;
            case "FloorGame":
                currentGame = new FloorGame1(new GameConfig { Maxiterations = 3, MaxLevel = 5, MaxPlayers = 5, MaxIterationTime = 20, ReductionTimeEachLevel = 2, NoOfControllers = 3, columns=14 }, 200);
                break;
            case "PatternBuilder":
                currentGame = new PatternBuilderGame(new GameConfig { Maxiterations = 3, MaxLevel = 3, MaxPlayers = 2, MaxIterationTime = 60, ReductionTimeEachLevel = 10, NoOfControllers = 3, columns=14 },2);
                break;
            //case "Lava":
            //    currentGame = new FloorIsLavaGame(new GameConfig { Maxiterations = 3, MaxLevel = 3, MaxPlayers = 2, MaxIterationTime = 60, ReductionTimeEachLevel = 10, NoOfControllers = 2, columns = 14 }, 5000, 5000, "AIzaSyDfOsv-WRB882U3W1ij-p3Io2xe5tSCRbI");
            //    break;
            case "Snakes":
                currentGame = new Snakes(new GameConfig { Maxiterations = 3, MaxLevel = 3, MaxPlayers = 2, MaxIterationTime = 60, ReductionTimeEachLevel = 10, NoOfControllers = 2, columns = 14 }, 5000, 5000, "AIzaSyDfOsv-WRB882U3W1ij-p3Io2xe5tSCRbI");
                break;
            case "Wipeout":
                currentGame = new WipeoutGame(new GameConfig { Maxiterations = 2, MaxLevel = 3, MaxPlayers = 5, MaxIterationTime = 60, ReductionTimeEachLevel = 20, NoOfControllers = 3, columns = 14 });
                break;
        }
        currentGame.LifeLineChanged += CurrentGame_LifeLineChanged; 
        currentGame.ScoreChanged += CurrentGame_ScoreChanged;
        currentGame.LevelChanged += CurrentGame_LevelChanged;
        currentGame.StatusChanged += CurrentGame_StatusChanged;
        currentGame?.StartGame();
    }

    private void CurrentGame_StatusChanged(object sender, string status)
    {
        if (lblStatus.InvokeRequired)
        {
            lblStatus.Invoke(new Action(() => lblStatus.Text = $"{status}"));
        }
    }

    private void CurrentGame_LevelChanged(object sender, int level)
    {
        if(lblLabel.InvokeRequired )
        {
            lblLabel.Invoke(new Action(() => lblLabel.Text = $"Level: {level}"));
        }
    }
    private void LoadPictures()
    {
       
          
               pictureBox1.BackgroundImage = Image.FromFile("content/heart_green.png");
         pictureBox2.BackgroundImage = Image.FromFile("content/heart_green.png");


        pictureBox3.BackgroundImage = Image.FromFile("content/heart_green.png");


        pictureBox4.BackgroundImage = Image.FromFile("content/heart_green.png");


        pictureBox5.BackgroundImage = Image.FromFile("content/heart_green.png");
          
    }
    private void CurrentGame_LifeLineChanged(object sender, int newLIfe)
    {
        if (newLIfe == 0)
         if (pictureBox1.InvokeRequired)
        {
            pictureBox1.Invoke(new Action(() => pictureBox1.BackgroundImage = Image.FromFile("content/heart_gray.png")));
        }
        if (newLIfe == 1)
        if (pictureBox2.InvokeRequired)
        {
            pictureBox2.Invoke(new Action(() => pictureBox2.BackgroundImage = Image.FromFile("content/heart_gray.png")));
        }
            if (newLIfe == 2)
            if (pictureBox3.InvokeRequired)
            {
                pictureBox3.Invoke(new Action(() => pictureBox3.BackgroundImage = Image.FromFile("content/heart_gray.png")));
            }
           if (newLIfe == 3)
            if (pictureBox4.InvokeRequired)
            {
                pictureBox4.Invoke(new Action(() => pictureBox4.BackgroundImage = Image.FromFile("content/heart_gray.png")));
            }
            if (newLIfe == 4)
            if (pictureBox5.InvokeRequired)
            {
                pictureBox5.Invoke(new Action(() => pictureBox5.BackgroundImage = Image.FromFile("content/heart_gray.png")));
            }
    }
    private void CurrentGame_ScoreChanged(object sender, int newScore)
    {
        if (lblScore1.InvokeRequired)
        {
            lblScore1.Invoke(new Action(() =>  lblScore1.Text = $"Score: {newScore}"));
        }
       // lblScore1.Text = $"Score: {newScore}";
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
            case "FloorGame":
                return "Players aim to step on the highlighted tiles as quickly as possible. The game lights up a set of target tiles, and players need to hit these targets within the given time.";
            case "PatternBuilder":
                return "Players must recreate a pattern based off memory as quickly as possible. Each correct pattern earns a point.";
            default:
                return "";
        }
    }

    private void ShowGameDescription(string gameTitle, string description)
    {
        txtGameDescription1.Text = $"{gameTitle}\r\n\r\n{description}";
    }

   

    private void InitializeStars()
    {
        stars = new List<Point>();
        for (int i = 0; i < 50; i++) // Create 50 stars
        {
            stars.Add(new Point(random.Next(this.Width), random.Next(this.Height)));
        }
    }

    private void StartStarAnimation()
    {
        starTimer = new Timer();
        starTimer.Interval = 50; // Update every 50 milliseconds
        starTimer.Tick += (sender, e) => MoveStars();
        starTimer.Start();
    }

    private void MoveStars()
    {
        for (int i = 0; i < stars.Count; i++)
        {
            stars[i] = new Point(stars[i].X, stars[i].Y + 5); // Move star down
            if (stars[i].Y > this.Height)
            {
                stars[i] = new Point(random.Next(this.Width), 0); // Reset star to top
            }
        }
        panel1.Invalidate();
    }

    private void MainForm_Paint(object sender, PaintEventArgs e)
    {
        ControlPaint.DrawBorder(e.Graphics, this.ClientRectangle,
            Color.LimeGreen, 10, ButtonBorderStyle.Solid, // Left
            Color.LimeGreen, 10, ButtonBorderStyle.Solid, // Top
            Color.LimeGreen, 10, ButtonBorderStyle.Solid, // Right
            Color.LimeGreen, 10, ButtonBorderStyle.Solid); // Bottom
    }

    private void panelBackground_Paint(object sender, PaintEventArgs e)
    {
        foreach (var star in stars)
        {
            e.Graphics.FillEllipse(Brushes.White, star.X, star.Y, 3, 3); // Draw star as white dot
        }
    }

    

    private void button1_Click(object sender, EventArgs e)
    {
        currentGame?.EndGame();
       
    }

    private void button2_Click(object sender, EventArgs e)
    {
        StartGame(comboBox1.SelectedItem.ToString());
    }
}
