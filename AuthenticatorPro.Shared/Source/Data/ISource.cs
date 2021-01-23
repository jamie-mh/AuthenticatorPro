using System.Collections.Generic;

namespace AuthenticatorPro.Shared.Source.Data
{
    public interface ISource<T>
    {
        public List<T> GetView();
        public List<T> GetAll();
        public T Get(int position);
    }
}