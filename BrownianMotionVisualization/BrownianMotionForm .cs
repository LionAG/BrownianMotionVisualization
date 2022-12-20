namespace BrownianMotionVisualization
{
    public partial class BrownianMotionForm : Form
    {
        public static readonly Random Random = new();

        /// <summary>
        /// Various shades of blue color, this should leave red text readable also with high particle count.
        /// </summary>
        static Brush RandomBrushColor => new SolidBrush(Color.FromArgb(30, 0, Random.Next(110, 256)));

        private readonly object _particlesLock = new();
        private readonly System.Timers.Timer _timer = new();
        private readonly List<Particle> _particles = new();

        // Particle settings

        private const int _particleSize = 7;
        private const int _initialParticleCount = 100;
        private const int _timerInterval = 25; // How often particle position will be updated.

        private bool _drawTrails = true;

        // Form settings

        private bool _fullscreen = false;

        // Set in the OnPaint override
        private bool _enableAntiAliasing = true;
        private bool _enableDoubleBuffering = true;

        private const int _initialFormWidth = 1280;
        private const int _initialFormHeight = 720;
        private const string _title = "Brownian Motion Visualization";

        private readonly Color _backgroundColor = Color.Black;
        private readonly Brush _textBrush = Brushes.Red;
        private Brush _particleBrush = RandomBrushColor;

        public BrownianMotionForm()
        {
            InitializeComponent();

            // Set up the form
            Size = new Size(_initialFormWidth, _initialFormHeight);
            Text = _title;
            BackColor = _backgroundColor;
            DoubleBuffered = _enableDoubleBuffering;

            CenterToScreen();

            // Add particles

            for (int v = 0; v < _initialParticleCount; v++)
            {
                AddParticle();
            }

            // Set up the timer to update the particle's position
            _timer.Interval = _timerInterval;
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();
        }

        private void RegenerateParticles()
        {
            lock(_particlesLock)
            {
                var count = _particles.Count;

                _particles.Clear();

                for (int v = 0; v < count; v++)
                {
                    AddParticle();
                }
            }
        }

        private void AddParticle()
        {
            lock (_particlesLock)
            {
                if (_particles.Count < 10000)
                {
                    var p = GenerateRandomPoint();

                    _particles.Add(new()
                    {
                        Brush = _particleBrush,
                        Size = _particleSize,
                        X = p.X,
                        Y = p.Y
                    });
                }
            }
        }

        private void RemoveParticle()
        {
            lock (_particlesLock)
            {
                if (_particles.Count > 0)
                    _particles.RemoveAt(0);
            }
        }

        private Point GenerateRandomPoint()
        {
            int x = Random.Next(0, this.Width);
            int y = Random.Next(0, this.Height);

            return new(x, y);
        }

        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            lock (_particlesLock)
            {
                // Create and start a task for each chunk of elements
                int chunkSize = 2000;
                List<Task> tasks = new();

                for (int i = 0; i < _particles.Count; i += chunkSize)
                {
                    int startIndex = i;
                    int endIndex = Math.Min(startIndex + chunkSize, _particles.Count);

                    tasks.Add(Task.Factory.StartNew(() =>
                    {
                        for (int v = startIndex; v < endIndex; v++)
                        {
                            var particleElement = _particles.ElementAt(v);

                            // Update the particle's position by a random amount
                            particleElement.X += Random.Next(-10, 11);
                            particleElement.Y += Random.Next(-10, 11);

                            // Keep the particle within the bounds of the form
                            if (particleElement.X < 0)
                            {
                                particleElement.X = 0;
                            }
                            else if (particleElement.X > Width - _particleSize)
                            {
                                particleElement.X = Width - _particleSize;
                            }

                            if (particleElement.Y < 0)
                            {
                                particleElement.Y = 0;
                            }
                            else if (particleElement.Y > Height - _particleSize)
                            {
                                particleElement.Y = Height - _particleSize;
                            }

                            particleElement.MovementStory.Add(new(particleElement.X, particleElement.Y));

                            if(particleElement.MovementStory.Count > 50)
                                particleElement.MovementStory.RemoveAt(0);
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
            }

            _particleBrush = RandomBrushColor;

            // Redraw the form
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Set draw properties

            if (_enableAntiAliasing)
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            DoubleBuffered = _enableDoubleBuffering;

            // Call base draw

            base.OnPaint(e);

            // Draw particles

            _particles.ForEach(p =>
            {
                // Draw the particle at its current position
                e.Graphics.FillEllipse(p.Brush, p.X, p.Y, p.Size, p.Size);

                // Draw movement history trail

                if (_drawTrails)
                {
                    if (p.MovementStory.Count > 1) // At least 2 points.
                        e.Graphics.DrawLines(new Pen(Color.DarkSeaGreen), p.MovementStory.ToArray());
                }
            });

            // Draw text

            Font font = new("Consolas", 11);

            e.Graphics.DrawString($"Particle count  [A/R/G]: {_particles.Count}",
                      font,
                      _textBrush,
                      new PointF(this.Width - 350, this.Height - 180));

            e.Graphics.DrawString($"Particle trails [T]:     {_drawTrails}",
                                  font,
                                  _textBrush,
                                  new PointF(this.Width - 350, this.Height - 160));

            e.Graphics.DrawString($"Window size   [F]: {Width}x{Height}",
                      font,
                      _textBrush,
                      new PointF(this.Width - 350, this.Height - 120));

            e.Graphics.DrawString($"Anti aliasing [S]: {_enableAntiAliasing}",
                                  font,
                                  _textBrush,
                                  new PointF(this.Width - 350, this.Height - 100));

            e.Graphics.DrawString($"Double buffer [D]: {_enableDoubleBuffering}",
                      font,
                      _textBrush,
                      new PointF(this.Width - 350, this.Height - 80));
        }

        private void BrownianMotionForm_KeyDown(object sender, KeyEventArgs e)
        {
            int particleModifierCount = 50;

            switch (e.KeyCode)
            {
                case Keys.A:
                    for (int x = 0; x < particleModifierCount; x++)
                        AddParticle();
                    break;
                case Keys.R:
                    for (int x = 0; x < particleModifierCount; x++)
                        RemoveParticle();
                    break;
                case Keys.G:
                    RegenerateParticles();
                    break;
                case Keys.F:
                    if (_fullscreen)
                    {
                        // exit

                        FormBorderStyle = FormBorderStyle.Sizable;
                        WindowState = FormWindowState.Normal;
                    }
                    else
                    {
                        // enter

                        FormBorderStyle = FormBorderStyle.None;
                        WindowState = FormWindowState.Maximized;
                    }

                    RegenerateParticles(); // To fill the current entire window space.

                    _fullscreen = !_fullscreen;
                    break;
                case Keys.S:
                    _enableAntiAliasing = !_enableAntiAliasing;
                    break;
                case Keys.D:
                    _enableDoubleBuffering = !_enableDoubleBuffering;
                    break;
                case Keys.T:
                    _drawTrails = !_drawTrails;
                    break;
                case Keys.Escape:
                    if(MessageBox.Show("Close the app?", "Notification", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk) == DialogResult.OK)
                        Close();
                    break;
            }
        }

        private void BrownianMotionForm_Load(object sender, EventArgs e)
        {

        }

        private void BrownianMotionForm_SizeChanged(object sender, EventArgs e)
        {
            RegenerateParticles();
        }
    }
}