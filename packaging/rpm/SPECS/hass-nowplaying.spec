Name: hass-nowplaying
Version: %{version}
Release: %{release}%{?dist}
Summary: Controls Home Assistant media players.
License: MIT
BuildArch: %{buildarch}

Requires: dotnet-runtime-8.0 >= 8.0.4

%define _build_id_links none
%global __strip /bin/true

%description
Controls Home Assistant media players.

%install
rm -rf $RPM_BUILD_ROOT

mkdir -p $RPM_BUILD_ROOT/%{_bindir}
mkdir -p $RPM_BUILD_ROOT/%{_datadir}/doc/%{name}
mkdir -p $RPM_BUILD_ROOT/%{_datadir}/%{name}

install -m 755 %{_sourcedir}/%{name} $RPM_BUILD_ROOT/%{_bindir}
install -m 644 %{_sourcedir}/%{name}.service $RPM_BUILD_ROOT/%{_datadir}/%{name}
install -m 644 %{_sourcedir}/appsettings.json $RPM_BUILD_ROOT/%{_datadir}/doc/%{name}
install -m 644 %{_sourcedir}/README.md $RPM_BUILD_ROOT/%{_datadir}/doc/%{name}
install -m 644 %{_sourcedir}/LICENSE $RPM_BUILD_ROOT/%{_datadir}/doc/%{name}

%files
%attr(0755, root, root) %{_bindir}/%{name}
%attr(0644, root, root) %{_datadir}/%{name}/%{name}.service
%config(noreplace) %{_datadir}/doc/%{name}/appsettings.json
%attr(0644, root, root) %{_datadir}/doc/%{name}/README.md
%attr(0644, root, root) %{_datadir}/doc/%{name}/LICENSE
%dir %{_datadir}/%{name}
%dir %{_datadir}/doc/%{name}

%post
echo "To configure the application, copy the included example configuration file to your .config directory:"
echo "mkdir -p ~/.config/hass-nowplaying/"
echo "cp /usr/share/doc/hass-nowplaying/appsettings.json ~/.config/hass-nowplaying/"

%preun
if [ $1 -eq 0 ]; then  # Only on uninstallation, not upgrade
    echo "If you configured the application to run as service, run the following to stop and disable it:"
    echo "systemctl --user stop hass-nowplaying.service"
    echo "systemctl --user disable hass-nowplaying.service"
    echo "rm ~/.config/systemd/user/hass-nowplaying.service"
    echo "systemctl --user daemon-reload"
fi

%postun
if [ $1 -eq 0 ] ; then
    echo "hass-nowplaying removed"
fi
