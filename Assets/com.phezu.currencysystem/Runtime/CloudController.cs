using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;

namespace Phezu.CurrencySystem {

    [AddComponentMenu("Phezu/Currency System/Cloud Controller")]
    public class CloudController {
        private bool mIsInitialized;

        /// <summary>
        /// Must be called before using any other functionality.
        /// </summary>
        public async void Initialize() {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            mIsInitialized = true;
        }

        /// <summary>
        /// Invalid if CloudController has not been initialized yet.
        /// </summary>
        /// <returns>null if key does not exist or exception occurs.</returns>
        public async Task<object> GetPlayerCurrency(string currencyID) {
            if (!mIsInitialized)
                return null;

            object amount = await RetrieveSpecificData<int>(currencyID);

            return amount;
        }

        /// <summary>
        /// Invalid if CloudController has not been initialized yet.
        /// </summary>
        public async void StorePlayerCurrency(string currencyID, int amount) {
            if (!mIsInitialized)
                return;

            await ForceSaveSingleData(currencyID, amount);
        }

        private async Task<object> RetrieveSpecificData<T>(string key) {
            try {
                var results = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { key });

                if (results.TryGetValue(key, out string value)) {
                    return typeof(T).IsPrimitive ? 
                        Convert.ChangeType(value, typeof(T)) : JsonUtility.FromJson<T>(value);
                }
                else {
                    Debug.Log($"There is no such key as {key}!");
                }
            }
            catch (CloudSaveValidationException e) {
                Debug.LogError(e);
            }
            catch (CloudSaveRateLimitedException e) {
                Debug.LogError(e);
            }
            catch (CloudSaveException e) {
                Debug.LogError(e);
            }

            return default;
        }

        private async Task ForceSaveSingleData<T>(string key, T value) {
            try {
                Dictionary<string, object> oneElement = new();

                string jsonValue = typeof(T).IsPrimitive ? value.ToString() : JsonUtility.ToJson(value);

                oneElement.Add(key, jsonValue);

                await CloudSaveService.Instance.Data.ForceSaveAsync(oneElement);

                Debug.Log($"Successfully saved {key}:{value}");
            }
            catch (CloudSaveValidationException e) {
                Debug.LogError(e);
            }
            catch (CloudSaveRateLimitedException e) {
                Debug.LogError(e);
            }
            catch (CloudSaveException e) {
                Debug.LogError(e);
            }
        }
    }

}