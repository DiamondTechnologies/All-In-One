#include "pch.h"
#include "AllInOneExplorerCommand.h"

#include <Windows.h>
#include <sal.h>
#include <wrl/module.h>

#pragma comment(lib, "RuntimeObject.lib")

using namespace Microsoft::WRL;

CoCreatableClass(AllInOneExplorerCommand);

_Use_decl_annotations_
STDAPI DllCanUnloadNow()
{
	return Module<InProc>::GetModule().Terminate()
		? S_OK
		: S_FALSE;
}

_Use_decl_annotations_
STDAPI DllGetClassObject(
	REFCLSID rclsid,
	REFIID riid,
	LPVOID* ppv)
{
	return Module<InProc>::GetModule()
		.GetClassObject(rclsid, riid, ppv);
}