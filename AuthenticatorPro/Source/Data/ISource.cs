using System.Collections.Generic;

namespace AuthenticatorPro.Data
{
    internal interface ISource<T>
    {
        public List<T> GetView();
        public List<T> GetAll();
        public T Get(int position);
    }
}