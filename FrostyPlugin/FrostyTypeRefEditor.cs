using Frosty.Controls;
using Frosty.Core.Windows;
using FrostySdk.Attributes;
using FrostySdk.Ebx;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FrostySdk;
using System.Reflection;

namespace Frosty.Core.Controls.Editors
{
    public class FrostyTypeRefEditor : FrostyTypeEditor<Button>
    {
        public FrostyTypeRefEditor()
        {
            ValueProperty = Button.ContentProperty;
        }

        private void OnButtonClick(FrostyPropertyGridItemData item)
        {
            ClassSelector selectNewType = new ClassSelector(TypeLibrary.GetConcreteTypes(), true);

            if (selectNewType.ShowDialog() == true)
            {
                Type type = selectNewType.SelectedClass;

                if (type != null)
                {
                    Guid typeGuid = type.GetCustomAttribute<GuidAttribute>().Guid;
                    item.Value = new TypeRef(typeGuid);
                }
            }
        }

        protected override void CustomizeEditor(Button editor, FrostyPropertyGridItemData item)
        {
            base.CustomizeEditor(editor, item);
            editor.Background = Brushes.Transparent;
            editor.HorizontalAlignment = HorizontalAlignment.Left;

            editor.Click += (o, e) => OnButtonClick(item);
        }
    }
}
