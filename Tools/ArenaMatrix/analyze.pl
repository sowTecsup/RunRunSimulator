#!/usr/bin/perl
use strict; use warnings;
my @only; my @files;
while (my $a = shift @ARGV) { if ($a eq '--only') { @only = split /,/, shift @ARGV; } else { push @files, $a; } }
die "uso: analyze.pl [--only A,B,C] matrix.csv [more.csv]\n" unless @files;
my %keep = map { $_ => 1 } @only;
my (%w, %l, %d, %margin, %games, %names, %cell, %fled, %hits, %pts, %seedw, %seedg, %post, %cellcount);
my @order; my %seeds;
for my $file (@files) {
    open(my $fh, "<", $file) or die "no abre $file";
    my $header = <$fh>;
    while (my $line = <$fh>) {
        chomp $line; next unless $line =~ /\S/;
        my ($i,$j,$p,$r,$seed,$ps,$rs,$winner,$secs,$pf,$rf,$ph,$rh,$pk,$rk,$pc,$rc,$detail) = split /;/, $line;
        next unless defined $rs;
        next if @only && (!$keep{$p} || !$keep{$r});
        $seeds{$seed}++;
        push @order, $p unless $names{$p}++;
        push @order, $r unless $names{$r}++;
        $cellcount{"$p|$r"}++; $cellcount{"$r|$p"}++ if $p ne $r;
        $cell{"$p|$r"} .= ($cell{"$p|$r"} ? " " : "") . "$ps-$rs";
        $cell{"$r|$p"} .= ($cell{"$r|$p"} ? " " : "") . "$rs-$ps" if $p ne $r;
        $games{$p}++; $games{$r}++ if $p ne $r;
        $seedg{"$p|$seed"}++; $seedg{"$r|$seed"}++ if $p ne $r;
        $margin{$p} += $ps - $rs; $margin{$r} += $rs - $ps if $p ne $r;
        $pts{$p} += $ps; $pts{$r} += $rs if $p ne $r;
        $fled{$p} += $pf; $fled{$r} += $rf if $p ne $r; $hits{$p} += $ph; $hits{$r} += $rh if $p ne $r;
        if ($ps > $rs) { $w{$p}++; $seedw{"$p|$seed"}++; if ($p ne $r) { $l{$r}++; } }
        elsif ($rs > $ps) { $l{$p}++; if ($p ne $r) { $w{$r}++; $seedw{"$r|$seed"}++; } }
        else { $d{$p}++; $seedw{"$p|$seed"} += 0.5; if ($p ne $r) { $d{$r}++; $seedw{"$r|$seed"} += 0.5; } }
        if (defined $detail) {
            while ($detail =~ /([PR]):([^=\s]+?)([CV])=(\d+)\/(\d+)\/(\d+)\/(\d+)\/(\d+)/g) {
                my $k = "$2$3"; $post{$k}{n}++; $post{$k}{s} += $4; $post{$k}{c} += $5; $post{$k}{h} += $6; $post{$k}{k} += $7; $post{$k}{f} += $8;
            }
        }
    }
    close $fh;
}
my @seedlist = sort { $a <=> $b } keys %seeds;
printf "%-18s %3s %3s %3s %7s %7s %7s %6s %6s", "plan", "W", "L", "D", "winrate", "margen", "puntos", "huidas", "golpes";
printf " %8s", "s$_" for @seedlist; print "\n";
for my $p (sort { (($w{$b}//0)+0.5*($d{$b}//0))/($games{$b}||1) <=> (($w{$a}//0)+0.5*($d{$a}//0))/($games{$a}||1) } @order) {
    my $g = $games{$p} || 1;
    my $wr = (($w{$p}//0) + 0.5*($d{$p}//0)) / $g;
    printf "%-18s %3d %3d %3d %6.0f%% %7.1f %7.1f %6.1f %6.1f", $p, $w{$p}//0, $l{$p}//0, $d{$p}//0, 100*$wr, $margin{$p}/$g, $pts{$p}/$g, $fled{$p}/$g, $hits{$p}/$g;
    for my $s (@seedlist) { my $sg = $seedg{"$p|$s"} || 0; printf " %7.0f%%", $sg ? 100*($seedw{"$p|$s"}//0)/$sg : 0; }
    print "\n";
}
print "\npor postura+sitio (promedio por criatura y ronda): n aseg min golpes caidas huidas\n";
for my $k (sort keys %post) { my $n = $post{$k}{n}; printf "%-8s n=%4d aseg=%5.2f min=%5.2f golpes=%4.2f caidas=%4.2f huidas=%4.2f\n", $k, $n, $post{$k}{s}/$n, $post{$k}{c}/$n, $post{$k}{h}/$n, $post{$k}{k}/$n, $post{$k}{f}/$n; }
print "\nmatriz (fila = jugador, columna = rival, marcador fila-columna; varios = varias rondas):\n";
printf "%-12s", "";
printf " %-13s", substr($_,0,13) for @order; print "\n";
for my $p (@order) {
    printf "%-12s", substr($p,0,12);
    for my $r (@order) { printf " %-13s", substr($cell{"$p|$r"} // "-", 0, 13); }
    print "\n";
}
