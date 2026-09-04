#include "pch.h"
#include "AllInOneExplorerCommand.h"

#include <appmodel.h>
#include <shlwapi.h>
#include <windows.h>

#include <ShObjIdl_core.h>
#include <string>
#include <vector>
#include <wrl/client.h>

#pragma comment(lib, "Shlwapi.lib")

namespace
{
	std::vector<std::wstring> GetPaths(
		IShellItemArray* items)
	{
		std::vector<std::wstring> result;

		if (!items)
			return result;

		DWORD count = 0;

		if (FAILED(items->GetCount(&count)))
			return result;

		result.reserve(count);

		for (DWORD i = 0; i < count; ++i)
		{
			ComPtr<IShellItem> item;

			if (FAILED(items->GetItemAt(i, &item)))
				continue;

			PWSTR path = nullptr;

			HRESULT hr = item->GetDisplayName(
				SIGDN_FILESYSPATH,
				&path);

			if (SUCCEEDED(hr) && path)
			{
				result.emplace_back(path);
				CoTaskMemFree(path);
			}
		}

		return result;
	}

	std::wstring GetPackagePath()
	{
		UINT32 length = 0;

		LONG result = GetCurrentPackagePath(
			&length,
			nullptr);

		if (result != ERROR_INSUFFICIENT_BUFFER ||
			length == 0)
		{
			return {};
		}

		std::vector<wchar_t> buffer(length);

		result = GetCurrentPackagePath(
			&length,
			buffer.data());

		if (result != ERROR_SUCCESS)
			return {};

		return std::wstring(buffer.data());
	}

	std::wstring GetApplicationPath()
	{
		std::wstring packagePath = GetPackagePath();

		if (packagePath.empty())
			return {};

		if (packagePath.back() != L'\\')
			packagePath += L'\\';

		packagePath += L"All In One.exe";

		return packagePath;
	}

	std::wstring QuoteCommandLineArgument(
		const std::wstring& value)
	{
		std::wstring result;
		result.reserve(value.size() + 2);

		result += L'"';

		size_t backslashes = 0;

		for (wchar_t ch : value)
		{
			if (ch == L'\\')
			{
				++backslashes;
				continue;
			}

			if (ch == L'"')
			{
				result.append(
					backslashes * 2 + 1,
					L'\\');

				result += L'"';
				backslashes = 0;
				continue;
			}

			result.append(
				backslashes,
				L'\\');

			backslashes = 0;
			result += ch;
		}
		result.append(
			backslashes * 2,
			L'\\');

		result += L'"';

		return result;
	}

	bool LaunchApplication(
		const std::wstring& executablePath,
		const std::vector<std::wstring>& paths)
	{
		if (executablePath.empty())
			return false;

		std::wstring commandLine;

		commandLine += QuoteCommandLineArgument(
			executablePath);

		for (const auto& path : paths)
		{
			commandLine += L' ';
			commandLine += QuoteCommandLineArgument(path);
		}

		std::vector<wchar_t> mutableCommandLine(
			commandLine.begin(),
			commandLine.end());

		mutableCommandLine.push_back(L'\0');

		STARTUPINFOW startupInfo{};
		startupInfo.cb = sizeof(startupInfo);

		PROCESS_INFORMATION processInfo{};

		BOOL success = CreateProcessW(
			executablePath.c_str(),
			mutableCommandLine.data(),
			nullptr,
			nullptr,
			FALSE,
			0,
			nullptr,
			nullptr,
			&startupInfo,
			&processInfo);

		if (!success)
			return false;

		CloseHandle(processInfo.hThread);
		CloseHandle(processInfo.hProcess);

		return true;
	}
}

IFACEMETHODIMP AllInOneExplorerCommand::GetTitle(
	IShellItemArray*,
	PWSTR* ppszName)
{
	if (!ppszName)
		return E_POINTER;
	return SHStrDupW(
		L"All In One",
		ppszName);
}

IFACEMETHODIMP AllInOneExplorerCommand::GetIcon(
	IShellItemArray*,
	PWSTR* ppszIcon)
{
	if (!ppszIcon)
		return E_POINTER;

	*ppszIcon = nullptr;

	std::wstring packagePath = GetPackagePath();

	if (packagePath.empty())
		return E_FAIL;

	if (packagePath.back() != L'\\')
		packagePath += L'\\';

	packagePath += L"Assets\\AppIcon.ico";
	packagePath += L",0";

	return SHStrDupW(
		packagePath.c_str(),
		ppszIcon);
}

IFACEMETHODIMP AllInOneExplorerCommand::GetToolTip(
	IShellItemArray*,
	PWSTR* ppszInfoTip)
{
	if (!ppszInfoTip)
		return E_POINTER;

	*ppszInfoTip = nullptr;

	return E_NOTIMPL;
}

IFACEMETHODIMP AllInOneExplorerCommand::GetCanonicalName(
	GUID* pguidCommandName)
{
	if (!pguidCommandName)
		return E_POINTER;

	*pguidCommandName = GUID_NULL;

	return S_OK;
}

IFACEMETHODIMP AllInOneExplorerCommand::GetState(
	IShellItemArray*,
	BOOL,
	EXPCMDSTATE* pCmdState)
{
	if (!pCmdState)
		return E_POINTER;

	*pCmdState = ECS_ENABLED;

	return S_OK;
}

IFACEMETHODIMP AllInOneExplorerCommand::Invoke(
	IShellItemArray* psiItemArray,
	IBindCtx*)
{
	auto paths = GetPaths(psiItemArray);

	if (paths.empty())
		return S_OK;

	auto executablePath = GetApplicationPath();

	if (executablePath.empty())
		return HRESULT_FROM_WIN32(
			ERROR_PATH_NOT_FOUND);

	if (!LaunchApplication(
		executablePath,
		paths))
	{
		return HRESULT_FROM_WIN32(
			GetLastError());
	}

	return S_OK;
}

IFACEMETHODIMP AllInOneExplorerCommand::GetFlags(
	EXPCMDFLAGS* pFlags)
{
	if (!pFlags)
		return E_POINTER;

	*pFlags = ECF_DEFAULT;

	return S_OK;
}

IFACEMETHODIMP AllInOneExplorerCommand::EnumSubCommands(
	IEnumExplorerCommand** ppEnum)
{
	if (!ppEnum)
		return E_POINTER;

	*ppEnum = nullptr;

	return E_NOTIMPL;
}