// UMA (Unity Multipurpose Avatar) Bridge Functions
// Extracted from unity-editor-bridge.js for modularity
import { sendCommand } from "./unity-editor-bridge.js";

export async function umaInspectFbx(params) {
  return sendCommand("uma/inspect-fbx", params);
}

export async function umaCreateSlot(params) {
  return sendCommand("uma/create-slot", params);
}

export async function umaCreateOverlay(params) {
  return sendCommand("uma/create-overlay", params);
}

export async function umaCreateWardrobeRecipe(params) {
  return sendCommand("uma/create-wardrobe-recipe", params);
}

export async function umaRegisterAssets(params) {
  return sendCommand("uma/register-assets", params);
}

export async function umaListGlobalLibrary(params) {
  return sendCommand("uma/list-global-library", params);
}

export async function umaListWardrobeSlots(params) {
  return sendCommand("uma/list-wardrobe-slots", params);
}

export async function umaListUMAMaterials(params) {
  return sendCommand("uma/list-uma-materials", params);
}

export async function umaGetProjectConfig(params) {
  return sendCommand("uma/get-project-config", params);
}

export async function umaVerifyRecipe(params) {
  return sendCommand("uma/verify-recipe", params);
}

export async function umaRebuildGlobalLibrary(params) {
  return sendCommand("uma/rebuild-global-library", params);
}

export async function umaCreateWardrobeFromFbx(params) {
  return sendCommand("uma/create-wardrobe-from-fbx", params);
}

export async function umaWardrobeEquip(params) {
  return sendCommand("uma/wardrobe-equip", params);
}

export async function umaEditRace(params) {
  return sendCommand("uma/edit-race", params);
}

export async function umaCreateRace(params) {
  return sendCommand("uma/create-race", params);
}
