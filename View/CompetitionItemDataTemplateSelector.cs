using Scoresheet.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Scoresheet.View
{
    public class CompetitionItemDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement element && item != null && item is CompetitionItem competitionItem)
            {

                if (competitionItem is Model.GroupItem)
                    return
                        element.FindResource("GroupItemTemplate") as DataTemplate;
                else
                    return
                        element.FindResource("SoloItemTemplate") as DataTemplate;
            }

            return null;
        }
    }
}
