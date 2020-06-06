namespace AuthenticatorPro
{
    interface IReorderableListAdapter
    {
        public void MoveItem(int oldPosition, int newPosition);
        public void NotifyMovementFinished();
    }
}