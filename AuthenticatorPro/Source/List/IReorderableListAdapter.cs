namespace AuthenticatorPro.List
{
    internal interface IReorderableListAdapter
    {
        public void MoveItem(int oldPosition, int newPosition);
        public void OnMovementStarted();
        public void OnMovementFinished();
    }
}