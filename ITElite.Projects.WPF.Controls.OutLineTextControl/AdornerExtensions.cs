using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace ITElite.Projects.WPF.Controls.TextControl
{
    public static class AdornerExtensions
    {
        public static void RemoveAdorners<T>(this AdornerLayer adr, UIElement elem)
        {
            Adorner[] adorners = adr.GetAdorners(elem);

            if (adorners == null) return;

            for (int i = adorners.Length - 1; i >= 0; i--)
            {
                if (adorners[i] is T)
                    adr.Remove(adorners[i]);
            }
        }

        public static bool Contains<T>(this AdornerLayer adr, UIElement elem)
        {
            Adorner[] adorners = adr.GetAdorners(elem);

            if (adorners == null) return false;

            for (int i = adorners.Length - 1; i >= 0; i--)
            {
                if (adorners[i] is T)
                    return true;
            }
            return false;
        }

        public static void RemoveAll(this AdornerLayer adr, UIElement elem)
        {
            try
            {
                Adorner[] adorners = adr.GetAdorners(elem);

                if (adorners == null) return;

                foreach (Adorner toRemove in adorners)
                    adr.Remove(toRemove);
            }
            catch { }
        }

        public static void RemoveAllRecursive(this AdornerLayer adr, UIElement element)
        {
            try
            {
                Action<UIElement> recurse = null;
                recurse = ((Action<UIElement>)delegate(UIElement elem)
                {
                    adr.RemoveAll(elem);
                    if (elem is Panel)
                    {
                        foreach (UIElement e in ((Panel)elem).Children)
                            recurse(e);
                    }
                    else if (elem is Decorator)
                    {
                        recurse(((Decorator)elem).Child);
                    }
                    else if (elem is ContentControl)
                    {
                        if (((ContentControl)elem).Content is UIElement)
                            recurse(((ContentControl)elem).Content as UIElement);
                    }

                });

                recurse(element);
            }
            catch { }
        }
    }

}
