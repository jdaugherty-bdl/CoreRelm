using Newtonsoft.Json;
using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models
{
    public class RelmModelApartment : RelmModel, IRelmModelApartment
    {
        [RelmColumn]
        public string ApartmentId { get; set; }

        [RelmColumn]
        public int UserId { get; set; }
        [RelmColumn]
        public string UserEmail { get; set; }
        [RelmColumn]
        public string UserName { get; set; }

        [RelmColumn]
        public IRelmMember Member { get; set; }

        /*
        private long _createUserId;
        private long _lastUpdatedUserId;
        private IMember _createUser;
        private IMember _lastUpdatedUser;

        [DALResolvable]
        public long LinkedCompanyId { get; set; }

        [DALResolvable]
        public long CreateUserId
        {
            get
            {
                return _createUserId;
            }
            set
            {
                _createUserId = value;

                if (_createUserId != _createUser?.Id)
                {
                    // force all reload
                    _createUser = null;
                    _ = CreateUser;
                }
            }
        }

        [DALResolvable]
        public long LastUpdatedUserId
        {
            get
            {
                return _lastUpdatedUserId;
            }
            set
            {
                _lastUpdatedUserId = value;

                if (_lastUpdatedUserId != _lastUpdatedUser?.Id)
                {
                    // force all reload
                    _lastUpdatedUser = null;
                    _ = LastUpdatedUser;
                }
            }
        }

        [JsonIgnore]
        public IMember CreateUser
        {
            get
            {
                if (_createUser == null) // singleton
                {
                    // force all reload
                    _createUser = MembershipHelper.GetMemberByUserId((int)_createUserId);
                }

                return _createUser;
            }
            set
            {
                _createUser = value;
                _createUserId = _createUser?.Id ?? default;
            }
        }

        [JsonIgnore]
        public IMember LastUpdatedUser
        {
            get
            {
                if (_lastUpdatedUser == null) // singleton
                {
                    // force all reload
                    _lastUpdatedUser = MembershipHelper.GetMemberByUserId((int)_lastUpdatedUserId);
                }

                return _lastUpdatedUser;
            }
            set
            {
                _lastUpdatedUser = value;
                _lastUpdatedUserId = _lastUpdatedUser?.Id ?? default;
            }
        }

        public CS_ApartmentedData SetCurrentMemberApartment(bool UpdateCreate = true)
        {
            var currentMemberId = MembershipHelper.GetCurrentMember()?.Id ?? 0;

            return SetMemberApartment(currentMemberId, UpdateCreate);
        }

        public CS_ApartmentedData SetMemberApartment(int MemberId, bool UpdateCreate = true)
        {
            if (UpdateCreate)
                CreateUserId = MemberId; // reload all data

            LastUpdatedUserId = MemberId; // reload all data
            return this;
        }
        */
        public IRelmModelApartment SetMemberApartment(int MemberId, bool UpdateCreate = true)
        {
            if (UpdateCreate)
                UserId = MemberId; // reload all data

            UserId = MemberId; // reload all data
            return this;
        }

        public RelmModelApartment() : base() { }
        public RelmModelApartment(DataRow ModelData, string AlternateTableName = null) : base(ModelData, AlternateTableName ?? nameof(RelmModelApartment)) { }
        public RelmModelApartment(IRelmModel fromModel) : base(fromModel) { }
    }
}
