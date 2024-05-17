namespace Tasks
{
    public class GettingUsedToTask : Task
    {
        public override void StartTask(Logger logger)
        {
            // start clock
        }

        protected override void EndTaskInternal(bool success)
        {
            throw new System.NotImplementedException();
        }

        public override void CleanTask()
        {
            throw new System.NotImplementedException();
        }

        public override bool Next()
        {
            // shouldn't be called i think
            return false;
        }
    }
}