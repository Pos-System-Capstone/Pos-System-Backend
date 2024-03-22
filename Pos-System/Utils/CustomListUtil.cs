namespace Pos_System.API.Utils
{
    public static class CustomListUtil
    {
        public static (List<Guid> idsToRemove, List<Guid> idsToAdd, List<Guid> idsToKeep) splitIdsToAddAndRemove(
            List<Guid> oldIds, List<Guid> newIds)
        {
            List<Guid> idsToAdd = new List<Guid>(newIds);
            List<Guid> idsToRemove = new List<Guid>(oldIds);
            List<Guid> idsToKeep = new List<Guid>();

            //A list of Id that is contain deleted ids but does not contain new ids added
            List<Guid> listWithOutIdsToAdd = new List<Guid>();

            //This logic help to split new ids, keep old ids and deleted ids
            newIds.ForEach(x =>
            {
                oldIds.ForEach(y =>
                {
                    if (x.Equals(y))
                    {
                        listWithOutIdsToAdd.Add(x);
                        idsToAdd.Remove(x);
                    }
                });
            });

            //This help clarify what ids to keep, using to update case
            idsToKeep = listWithOutIdsToAdd;

            //This logic help to remove old ids, keep only ids to remove
            oldIds.ForEach(x =>
            {
                listWithOutIdsToAdd.ForEach(y =>
                {
                    if (x.Equals(y)) idsToRemove.Remove(x);
                });
            });

            return (idsToRemove, idsToAdd, idsToKeep);
        }
    }
}