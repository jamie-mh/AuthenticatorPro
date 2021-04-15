using System.Collections.Generic;

namespace AuthenticatorPro.Shared.Data
{
    public interface ISource<T>
    {
        public List<T> GetView();
        public List<T> GetAll();
        public T Get(int position);
    }
}