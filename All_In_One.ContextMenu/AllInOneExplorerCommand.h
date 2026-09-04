#pragma once

#include <ShObjIdl_core.h>
#include <windows.h>
#include <wrl/implements.h>

using namespace Microsoft::WRL;

class __declspec(uuid("AA5E78EB-6B1C-4B4B-8D16-D31F4519856A"))
	AllInOneExplorerCommand final
	: public RuntimeClass<
	RuntimeClassFlags<ClassicCom>,
	IExplorerCommand>
{
public:
	IFACEMETHODIMP GetTitle(
		IShellItemArray* psiItemArray,
		PWSTR* ppszName) override;

	IFACEMETHODIMP GetIcon(
		IShellItemArray* psiItemArray,
		PWSTR* ppszIcon) override;

	IFACEMETHODIMP GetToolTip(
		IShellItemArray* psiItemArray,
		PWSTR* ppszInfoTip) override;

	IFACEMETHODIMP GetCanonicalName(
		GUID* pguidCommandName) override;

	IFACEMETHODIMP GetState(
		IShellItemArray* psiItemArray,
		BOOL fOkToBeSlow,
		EXPCMDSTATE* pCmdState) override;

	IFACEMETHODIMP Invoke(
		IShellItemArray* psiItemArray,
		IBindCtx* pbc) override;

	IFACEMETHODIMP GetFlags(
		EXPCMDFLAGS* pFlags) override;

	IFACEMETHODIMP EnumSubCommands(
		IEnumExplorerCommand** ppEnum) override;
};