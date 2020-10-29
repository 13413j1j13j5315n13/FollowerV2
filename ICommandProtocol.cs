namespace FollowerV2
{
    internal interface ICommandProtocol
    {
        void Start();
        void Restart();
        void Stop();
        void Work(NetworkActivityObject obj);
    }
}