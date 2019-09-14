using Paint.Utils;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Paint
{


    public class AllPath
    {
        public Dictionary<long, SKPath> TemporaryPaths { get; set; } = new Dictionary<long, SKPath>();
        public List<SKPath> Paths { get; set; } = new List<SKPath>();
    }

    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        private LinkedList<AllPath> AllPaths = new LinkedList<AllPath>();
        private LinkedListNode<AllPath> current = null;

        private readonly MediaHelper mediaHelper = new MediaHelper();

        private MediaHelper.ImagePhoto imageLoaded;


        private Dictionary<long, SKPath> temporaryPaths = new Dictionary<long, SKPath>();
        private List<SKPath> paths = new List<SKPath>();
        private SKCanvas canvas;
        private SKCanvasView senderSK;
        private SKSurface surface;
        SKData pngImage;

        public MainPage()
        {
            InitializeComponent();
        }


        private void OnPainting(object sender, SKPaintSurfaceEventArgs args)
        {
            try
            {
                // CLEARING THE SURFACE

                // we get the current surface from the event args
                surface = args.Surface;
                // then we get the canvas that we can draw on

                canvas = surface.Canvas;
                //// clear the canvas / view
                canvas.Clear(SKColors.White);

                if (imageLoaded != null)
                {
                    var libraryBitmap = SKBitmap.Decode(imageLoaded.Path);
                    SKImageInfo info = args.Info;

                    float x = (info.Width - libraryBitmap.Width) / 2;
                    float y = (info.Height / 3 - libraryBitmap.Height) / 2;

                    canvas.DrawBitmap(libraryBitmap, x, y);
                }


                // create the paint for the touch path
                var touchPathStroke = new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.Purple,
                    StrokeWidth = 5
                };

                // draw the paths
                foreach (var touchPath in temporaryPaths)
                {
                    canvas.DrawPath(touchPath.Value, touchPathStroke);
                }
                foreach (var touchPath in paths)
                {
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
                        var p = new SKPath();
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

                        if (current == null)
                            current = AllPaths.AddFirst(AddPathFile(e));
                        else
                        {
                            if (current.Next != null)
                                RemoveAllNode(current.Next);

                            current = AllPaths.AddLast(AddPathFile(e));
                        }


                        break;
                    case SKTouchAction.Cancelled:
                        // we don't want that stroke
                        temporaryPaths.Remove(e.Id);
                        break;
                }

                // we have handled these events
                e.Handled = true;

                senderSK = ((SKCanvasView)sender);
                // update the UI
                senderSK.InvalidateSurface();
            }
            catch (Exception ex)
            {

            }
        }

        private AllPath AddPathFile(SKTouchEventArgs e)
        {
            var a = paths.ToArray().ToList();
            a.Add(temporaryPaths[e.Id]);
            temporaryPaths.Remove(e.Id);

            var newNode = new AllPath()
            {
                Paths = a,
                TemporaryPaths = new Dictionary<long, SKPath>(temporaryPaths)
            };
            paths = a.ToArray().ToList();
            return newNode;
        }

        private void RemoveAllNode(LinkedListNode<AllPath> current)
        {
            var nextNode = current.Next;
            AllPaths.Remove(current);

            if (nextNode != null)
                RemoveAllNode(nextNode);
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            temporaryPaths = new Dictionary<long, SKPath>();
            paths = new List<SKPath>();
            AllPaths.Clear();
            current = null;
            imageLoaded = null;
            senderSK.InvalidateSurface();
        }

        private void BtnBack_Clicked(object sender, EventArgs e)
        {
            current = current?.Previous;
            SetPath(current);
        }

        private void SetPath(LinkedListNode<AllPath> current)
        {
            if (current != null)
            {
                temporaryPaths = current.Value.TemporaryPaths;
                paths = current.Value.Paths;
                senderSK.InvalidateSurface();
            }
        }

        private void BtnNext_Clicked(object sender, EventArgs e)
        {
            current = current?.Next;
            SetPath(current);
        }

        private async void Button_Clicked_1(object sender, EventArgs e)
        {
            imageLoaded = await mediaHelper.TakePhotoAsync();

            if (senderSK != null)
                senderSK.InvalidateSurface();
        }

        private async void Button_Clicked_2(object sender, EventArgs e)
        {
            byte[] data = pngImage.ToArray();
            await mediaHelper.SaveImage(data, "L", "emy.jpg");
        }
    }
}
