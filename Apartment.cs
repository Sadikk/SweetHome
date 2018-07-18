using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SweetHome
{
    public class Apartment : INotifyPropertyChanged
    {
        private string _price;

        public string Price
        {
            get { return _price; }
            set { _price = value;
                OnPropertyChanged();
            }
        }

        private string _area;

        public string Area
        {
            get { return _area; }
            set { _area = value;
                OnPropertyChanged();
            }
        }

        private string _bedRooms;

        public string Bedrooms
        {
            get { return _bedRooms; }
            set { _bedRooms = value;
                OnPropertyChanged();
            }
        }

        private string _rooms;

        public string Rooms
        {
            get { return _rooms; }
            set { _rooms = value;
                OnPropertyChanged();
            }
        }

        private string _localisation;

        public string Localisation
        {
            get { return _localisation; }
            set { _localisation = value;
                OnPropertyChanged();
            }
        }

        private string _link;

        public string Link
        {
            get { return _link; }
            set { _link = value;
                OnPropertyChanged();
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
