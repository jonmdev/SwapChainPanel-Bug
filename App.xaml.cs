using Microsoft.Maui.Platform;
using System.Diagnostics;


#if WINDOWS

//=========================================================================================
//WINDOWS | CODE WORKS FINE WITH CALENDARVIEW OR ANY OTHER BUT BREAKS WITH SWAPCHAINPANELS
//=========================================================================================
//using TYPETOADD = Microsoft.UI.Xaml.Controls.CalendarView; //=====WORKS OKAY
using TYPETOADD = Microsoft.UI.Xaml.Controls.SwapChainPanel; //======DOES NOT WORK, GIVES ERROR
//using TYPETOADD = Microsoft.UI.Xaml.Controls.SwapChainBackgroundPanel; //======DOES NOT WORK, GIVES ERROR


#else
using TYPETOADD = object;
#endif

namespace SwapChainPanel_Bug {
    public partial class App : Application {
        public App() {
            InitializeComponent();

            MainPage = new AppShell();

            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;

            void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e) {
                System.Diagnostics.Debug.WriteLine($"********************************** UNHANDLED EXCEPTION! Details: {e.Exception.ToString()}  {e.ToString()}");
            }

            ContentPage mainPage = new ContentPage();
            this.MainPage = mainPage;

            AbsoluteLayout abs1 = new();
            AbsoluteLayout abs2 = new();
            mainPage.Content = abs1;
            abs1.Add(abs2);
            Debug.WriteLine("ABOUT TO CREATE URHO SURFACE");
            UrhoSurface newUrhoSurface = new UrhoSurface();
            mainPage.HandlerChanged += delegate {
                Debug.WriteLine("ASSIGN HANDLER");
                if (mainPage.Handler.MauiContext != null) {

                    //https://github.com/microsoft/microsoft-ui-xaml/issues/1806
                    newUrhoSurface.ToHandler(mainPage.Handler.MauiContext); //CANNOT PERFORM DUE TO:
                                                                            //Exception thrown: 'System.Runtime.InteropServices.COMException' in WinRT.Runtime.dll
                                                                            //WinRT information: Setting 'Background' property is not supported on SwapChainPanel.

                    abs2.Add(newUrhoSurface);  //thus cannot also add to visual layout

                }
            };

        }
    }

    public interface IUrhoSurface : IView {
    }
    public class UrhoSurface : Microsoft.Maui.Controls.View, IUrhoSurface {
        //The control should provide a public API that will be accessed by its handler, and control consumers.Cross-platform controls should derive from View, which represents a visual element that's used to place layouts and views on the screen.
    }

#if WINDOWS
    public partial class MauiUrhoSurface : TYPETOADD {
        bool inited;
        TaskCompletionSource<bool> loadedTaskSource;
        UrhoSurface urhoSurface;

        public MauiUrhoSurface(UrhoSurface urhoSurface) {
            Debug.WriteLine("MauiUrhoSurface | MAUI URHO SURFACE CONSTRUCTOR RUN");
            this.urhoSurface = urhoSurface; //SAVE IT FOR SOMETHING??? CHECK IT FOR CHAGNES ETC???

            //Opacity = 0;
            loadedTaskSource = new TaskCompletionSource<bool>();

            //Loaded += (s, e) => loadedTaskSource.TrySetResult(true);
            Loaded += UrhoSurface_Loaded;
            Unloaded += UrhoSurface_Unloaded;
            SizeChanged += UrhoSurface_SizeChanged;
        }
        void UrhoSurface_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
            Debug.WriteLine("MauiUrhoSurface | LOADED URHO SURFACE");
            bool result = loadedTaskSource.TrySetResult(true);
            Debug.WriteLine("MauiUrhoSurface | LOADED SUCESSFUL: " + result);
        }

        void UrhoSurface_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e) {
            Debug.WriteLine("MauiUrhoSurface | URHO SURFACE SIZE CHANGED");
            if (!inited)
                return;
        }
        void UrhoSurface_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
            Debug.WriteLine("MauiUrhoSurface | URHO SURFACE UNLOADED");
        }

    }

#elif ANDROID
    public partial class MauiUrhoSurface : Android.Widget.FrameLayout {
        public MauiUrhoSurface(Android.Content.Context context) : base(context) {
        }
    }
#elif IOS || MACCATALYST
    public partial class MauiUrhoSurface : UIKit.UIView {
        private UrhoSurface virtualView;

        public MauiUrhoSurface(UrhoSurface virtualView) {
            this.virtualView = virtualView;
        }
    }
#else
public partial class MauiUrhoSurface : object {
}
#endif
    public partial class UrhoSurfaceHandler : Microsoft.Maui.Handlers.ViewHandler<UrhoSurface, MauiUrhoSurface> {
        public static IPropertyMapper<UrhoSurface, UrhoSurfaceHandler> PropertyMapper = new PropertyMapper<UrhoSurface, UrhoSurfaceHandler>(UrhoSurfaceHandler.ViewMapper) {
        };
        public UrhoSurfaceHandler() : base(PropertyMapper) {
            Debug.WriteLine("UrhoSurfaceHandler | HANDLER CREATED");
        }

        protected override MauiUrhoSurface CreatePlatformView() {
            Debug.WriteLine("UrhoSurfaceHandler | HANDLER CREATES PLATFORM VIEW");
            //where you do per platform set up 
#if ANDROID
return null;
#else
            return new MauiUrhoSurface(VirtualView);
#endif
        }
        protected override void ConnectHandler(MauiUrhoSurface platformView) {
            Debug.WriteLine("UrhoSurfaceHandler | HANDLER TRYING TO CONNECT");
            base.ConnectHandler(platformView);
            Debug.WriteLine("UrhoSurfaceHandler | HANDLER CONNECTED");
            Debug.WriteLineIf(platformView != null, "UrhoSurfaceHandler | platform type: " + platformView.GetType());

            // Perform any control setup here
        }

        protected override void DisconnectHandler(MauiUrhoSurface platformView) {
            //platformView.Dispose();
            Debug.WriteLine("UrhoSurfaceHandler | DISCONNECT HANDLER");
            base.DisconnectHandler(platformView);
        }
    }


}
