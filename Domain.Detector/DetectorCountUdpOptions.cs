// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

namespace Econolite.Ode.Domain.Detector;

public class DetectorCountUdpOptions
{
    public const string Section = "DetectorCountUdp";

    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 9098;
}