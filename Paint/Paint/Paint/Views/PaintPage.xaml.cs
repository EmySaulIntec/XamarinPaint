using Paint.Utils;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Paint.Views
{
    public class SkPathExtended : SKPath
    {
        public SKColor Color { get; set; }
        public SkPathExtended(SKColor sKColor)
        {
            this.Color = sKColor;
        }
    }

    public class SkCanvasResult
    {
        public List<SkPathExtended> Paths { get; set; } = new List<SkPathExtended>();
    }

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PaintPage : ContentPage
    {
        private readonly MediaHelper mediaHelper = new MediaHelper();

        private MediaHelper.ImagePhoto imageLoaded;
        private SKData pngImage;
        SKColor Currentcolor;

        public int currentStateIndex { get; set; }
        List<SkCanvasResult> allCanvasResults = new List<SkCanvasResult>();
        private Dictionary<long, SkPathExtended> temporaryPaths { get; set; } = new Dictionary<long, SkPathExtended>();
        private List<SkPathExtended> paths { get; set; } = new List<SkPathExtended>();
        public PaintPage()
        {
            InitializeComponent();
            Currentcolor = SKColors.Black;
            Init();
        }

        private void Init()
        {
            paths.Clear();
            temporaryPaths.Clear();
            allCanvasResults.Clear();
            allCanvasResults.Add(new SkCanvasResult());
            imageLoaded = null;

            skC.InvalidateSurface();

        }

        private void OnPainting(object sender, SKPaintSurfaceEventArgs args)
        {
            try
            {
                // CLEARING THE SURFACE

                // we get the current surface from the event args
                SKSurface surface = args.Surface;
                // then we get the canvas that we can draw on

                var canvas = surface.Canvas;

                //// clear the canvas / view
                canvas.Clear(SKColors.White);

                if (imageLoaded != null)
                {
                    var libraryBitmap = SKBitmap.Decode(imageLoaded.Path);
                    SKImageInfo info = args.Info;

                    float x = (info.Width - libraryBitmap.Width) / 2 ;
                    float y = (info.Height / 2 - libraryBitmap.Height) / 2;

                    canvas.DrawBitmap(libraryBitmap, x, y);
                }


                // create the paint for the touch path
                var touchPathStroke = new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 5
                };

                // draw the paths
                foreach (var touchPath in temporaryPaths)
                {
                    touchPathStroke.Color = touchPath.Value.Color;
                    canvas.DrawPath(touchPath.Value, touchPathStroke);
                }
                foreach (var touchPath in paths)
                {
                    touchPathStroke.Color = touchPath.Color;
                    canvas.DrawPath(touchPath, touchPathStroke);
                }

                var snap = surface.Snapshot();
                pngImage = snap.Encode();
            }
            catch (Exception ex)
            {

            }
        }


        private void OnTouch(object sender, SKTouchEventArgs e)
        {
            try
            {
                switch (e.ActionType)
                {
                    case SKTouchAction.Pressed:
                        // start of a stroke
                        var p = new SkPathExtended(Currentcolor);
                        p.MoveTo(e.Location);
                        temporaryPaths[e.Id] = p;
                        break;
                    case SKTouchAction.Moved:
                        // the stroke, while pressed
                        if (e.InContact)
                            temporaryPaths[e.Id].LineTo(e.Location);
                        break;
                    case SKTouchAction.Released:
                        // end of a stroke

                        paths.Add(temporaryPaths[e.Id]);
                        temporaryPaths.Remove(e.Id);

                        if (currentStateIndex < allCanvasResults.Count)
                        {
                            int count = allCanvasResults.Count - (currentStateIndex + 1);
                            allCanvasResults.RemoveRange(currentStateIndex + 1, count);
                            currentStateIndex = allCanvasResults.Count;
                        }
                        else
                        {
                            currentStateIndex++;
                        }

                        allCanvasResults.Add(new SkCanvasResult()
                        {
                            Paths = paths.ToList(),
                        });



                        break;
                    case SKTouchAction.Cancelled:
                        // we don't want that stroke
                        temporaryPaths.Remove(e.Id);
                        break;
                }

                // we have handled these events
                e.Handled = true;

                var senderSK = ((SKCanvasView)sender);
                // update the UI
                senderSK.InvalidateSurface();
            }
            catch (Exception ex)
            {

            }
        }


        private async void Button_Clicked(object sender, EventArgs e)
        {
            imageLoaded = await mediaHelper.TakePhotoAsync(false, Plugin.Media.Abstractions.PhotoSize.Large);
            skC.InvalidateSurface();
        }

        private void Button_Clicked_1(object sender, EventArgs e)
        {
            var btn = ((Button)sender);
            switch (btn.Text)
            {
                case "Red":
                    Currentcolor = SKColors.Red;
                    break;
                case "Blue":
                    Currentcolor = SKColors.Blue;
                    break;
                case "Gray":
                    Currentcolor = SKColors.Gray;
                    break;
            }

        }

        private void BtnNex_Clicked(object sender, EventArgs e)
        {
            if (currentStateIndex < allCanvasResults.Count - 1)
            {
                currentStateIndex++;
                paths = allCanvasResults[currentStateIndex].Paths.ToList();
                temporaryPaths.Clear();
            }
            skC.InvalidateSurface();
        }

        private void BtnBack_Clicked(object sender, EventArgs e)
        {
            if (currentStateIndex >= allCanvasResults.Count)
            {
                currentStateIndex = allCanvasResults.Count - 1;
            }
            if (currentStateIndex > 0)
            {
                currentStateIndex--;
                paths = allCanvasResults[currentStateIndex].Paths.ToList();
                temporaryPaths.Clear();
            }
            skC.InvalidateSurface();
        }

        private void BtnClear_Clicked(object sender, EventArgs e)
        {
            Init();
        }

        private async void BtnSave_Clicked(object sender, EventArgs e)
        {
            if (pngImage != null)
            {
                byte[] data = pngImage.ToArray();
                await mediaHelper.SaveImage(data, "L", "emy.jpg");
                await DisplayAlert("Alert", "Image saved.", "Ok");
            }
            else
            {
                await DisplayAlert("Alert", "Image no saved.", "Ok");
            }
        }
    }
}