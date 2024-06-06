using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scoresheet.Formatter
{
    public class FormSubmissionColumn : ObservableObject
    {
        public string Header { get; private set; }

        private ColumnType _ColumnType = ColumnType.Ignore;
        /// <summary>
        /// Gets or sets the column type of this column
        /// </summary>
        public ColumnType ColumnType
        {
            get => _ColumnType;
            set => SetProperty(ref _ColumnType, value);
        }

        public FormSubmissionColumn(string header)
        {
            Header = header;

            string[] columnTypeStrings = Enum.GetNames(typeof(Scoresheet.Formatter.ColumnType));
            for (int i = 0; i < columnTypeStrings.Length; i++)
            {
                string possibleType = columnTypeStrings[i];
                if (header.ToUpperInvariant().Replace(" ","").Contains(possibleType.ToUpperInvariant()))
                {
                    ColumnType = (ColumnType)i;
                    break;
                }
            }
        }
    }

    public enum ColumnType
    {
        Ignore,
        Timestamp,
        Email,
        PhoneNumber,
        Name,
        Year,
        SoloItems,
        GroupItems,
    }
}
