using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;

namespace OpenFTTH.UtilityGraphService.API.Queries
{
    public class GetEquipmentDetails : IQuery<Result<GetEquipmentDetailsResult>>
    {
        public InterestIdList InterestIdsToQuery { get; }

        public EquipmentIdList EquipmentIdsToQuery { get; }


        /// <summary>
        /// Use this contructor, if you want to query by equipment ids
        /// </summary>
        /// <param name="equipmentIds"></param>
        public GetEquipmentDetails(EquipmentIdList equipmentIds)
        {
            if (equipmentIds == null || equipmentIds.Count == 0)
                throw new ArgumentException("At least one equipment id must be specified");

            this.InterestIdsToQuery = new InterestIdList();

            this.EquipmentIdsToQuery = equipmentIds;
        }


        /// <summary>
        /// Use this contructor, if you want to query by interest ids
        /// </summary>
        /// <param name="interestIds"></param>
        public GetEquipmentDetails(InterestIdList interestIds)
        {
            if (interestIds == null || interestIds.Count == 0)
                throw new ArgumentException("At least one interest id must be specified");

            this.EquipmentIdsToQuery = new EquipmentIdList();

            this.InterestIdsToQuery = interestIds;
        }
    }
}
