#!/usr/bin/perl
use strict; use warnings;
my @pairs = ("Senuelos|Jauria","HormigueroSenuelo|Jauria","Engano|Jauria","Emboscada|Jauria","Senuelos|Fortin","HormigueroSenuelo|Fortin","Engano|Muralla","Senuelos|Muralla","Emboscada|Muralla","Hormiguero|Jauria","Muralla|Jauria","Codicia|Jauria","Senuelos|Hormiguero","HormigueroSenuelo|Hormiguero");
for my $file (@ARGV) {
    my %res;
    open(my $fh, "<", $file) or die; my $h = <$fh>;
    while (<$fh>) { chomp; my @c = split /;/; next unless defined $c[6]; my ($p,$r,$ps,$rs) = @c[2,3,5,6];
        for my $pair (@pairs) { my ($a,$b) = split /\|/, $pair;
            if ($p eq $a && $r eq $b) { push @{$res{$pair}}, [$ps,$rs]; }
            elsif ($p eq $b && $r eq $a) { push @{$res{$pair}}, [$rs,$ps]; } } }
    close $fh;
    print "== $file\n";
    for my $pair (@pairs) { my @g = @{$res{$pair} // []}; my ($w,$pts,$opp) = (0,0,0);
        for my $g (@g) { $w++ if $g->[0] > $g->[1]; $w += 0.5 if $g->[0] == $g->[1]; $pts += $g->[0]; $opp += $g->[1]; }
        my $n = @g || 1; printf "  %-28s gana %.0f%%  puntos %.1f vs %.1f  (%s)\n", $pair, 100*$w/$n, $pts/$n, $opp/$n, join(" ", map { "$_->[0]-$_->[1]" } @g); }
}
